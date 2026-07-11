using Game.Core;
using Game.Core.Affix;

namespace Game.Core.Battle;

public sealed class BattleHookExecutor
{
    internal BattleEffectExecutor? EffectExecutor { get; set; }

    public void Execute(BattleHookContext context, HookAffix hook)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hook);

        if (hook.Timing != context.Timing)
        {
            throw new InvalidOperationException(
                $"Hook timing '{hook.Timing}' does not match context timing '{context.Timing}'.");
        }

        BattleHookPreviewPolicy.EnsureSafe(context, hook);
        if (hook.Conditions.Any(condition => !BattleHookEvaluator.Evaluate(context, condition)))
        {
            return;
        }

        foreach (var effect in hook.Effects)
        {
            if (!BattleEffectTimingPolicy.Supports(context.Timing, effect))
            {
                throw new InvalidOperationException(
                    $"Effect '{effect.GetType().Name}' cannot execute at timing '{context.Timing}'.");
            }

            using var effectScope = context.State.EnterEffect($"hook:{hook.Timing}:{effect.GetType().Name}");
            (EffectExecutor ?? throw new InvalidOperationException(
                "Battle hook executor is not attached to a battle engine."))
                .ExecuteHook(context, effect);
        }

        TryRequestFloatText(context, hook.FloatText);
        TryRequestSpeech(context, hook.Speech);
    }

    private static void TryRequestFloatText(BattleHookContext context, BattleFloatTextDefinition? floatText)
    {
        if (floatText is null)
        {
            return;
        }

        var target = floatText.Target switch
        {
            BattleFloatTextTarget.Owner => context.Unit,
            BattleFloatTextTarget.Source => context.Source,
            BattleFloatTextTarget.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(floatText.Target), floatText.Target, null),
        };
        var text = BattleCueTextFormatter.Format(floatText.Text, context.Unit, context.Source, context.Target);
        if (target is not null && !string.IsNullOrWhiteSpace(text))
        {
            context.RequestFloatText(target, text, floatText.Style);
        }
    }

    private static void TryRequestSpeech(BattleHookContext context, BattleSpeechDefinition? speech)
    {
        if (speech is null)
        {
            return;
        }

        var speaker = ResolveSpeaker(context, speech.Speaker);
        var line = BattleSpeechRuntime.TryPickLine(speech, context.Random);
        line = BattleCueTextFormatter.Format(line, context.Unit, context.Source, context.Target);
        BattleSpeechRuntime.TryEmit(context.State, speaker, line, context.Timing);
    }

    private static BattleUnit? ResolveSpeaker(BattleHookContext context, BattleSpeechSpeaker speaker) =>
        speaker switch
        {
            BattleSpeechSpeaker.Owner => context.Unit,
            BattleSpeechSpeaker.Source => context.Source,
            BattleSpeechSpeaker.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(speaker), speaker, null),
        };
}
