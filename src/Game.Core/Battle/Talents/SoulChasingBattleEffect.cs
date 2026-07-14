using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record SoulChasingBattleEffectParameters(
    [property: NotWhiteSpace] string BuffId,
    [property: Probability] double SpeechChance,
    IReadOnlyList<string> SpeechLines);

internal sealed class SoulChasingBattleEffectHandler
    : CustomBattleEffectHandler<SoulChasingBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Execute(
        IHitConfirmedEffectContext context,
        SoulChasingBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null)
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

        var existing = context.Target.TryGetBuff(parameters.BuffId);
        context.ApplyBuff(
            context.Target,
            parameters.BuffId,
            level: Math.Min(10, checked((existing?.Level ?? 0) + 1)),
            duration: 4,
            detail: parameters.BuffId);
    }
}
