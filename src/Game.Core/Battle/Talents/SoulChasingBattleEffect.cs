using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record SoulChasingBattleEffectParameters(
    string BuffId,
    double SpeechChance,
    IReadOnlyList<string> SpeechLines);

internal sealed class SoulChasingBattleEffectHandler
    : CustomBattleEffectHandler<SoulChasingBattleEffectParameters, IBattleEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(SoulChasingBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.BuffId);
        if (parameters.SpeechChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Soul chasing speech chance must be between 0 and 1.");
        }
    }

    public override void Execute(
        IBattleEffectContext context,
        SoulChasingBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            context.Source is null || context.Target is null)
        {
            return;
        }

        var chance = Math.Clamp(
            (context.Source.GetStat(StatType.Fuyuan) - context.Target.GetStat(StatType.Dingli)) / 100d,
            0d,
            1d);
        if (!Probability.RollChance(context.Random, chance))
        {
            return;
        }

        var speech = BattleSpeechRuntime.TryPickLine(
            new BattleSpeechDefinition
            {
                Lines = parameters.SpeechLines,
                Chance = parameters.SpeechChance,
            },
            context.Random);
        if (speech is not null)
        {
            context.RequestSpeech(context.Unit, speech);
        }

        var resolver = hookContext.Engine.BuffResolver;
        var definition = resolver.Resolve(parameters.BuffId);
        var existing = context.Target.TryGetBuff(parameters.BuffId);
        resolver.Apply(
            context.State,
            context.Source,
            context.Target,
            definition,
            level: Math.Min(10, checked((existing?.Level ?? 0) + 1)),
            duration: 4,
            detail: parameters.BuffId,
            timing: context.Timing);
    }
}
