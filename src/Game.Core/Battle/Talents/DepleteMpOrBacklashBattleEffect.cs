using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Implements the two resource branches of 神.化功大法 using the engine's
/// direct HP/MP mutation APIs.
/// </summary>
public sealed record DepleteMpOrBacklashBattleEffectParameters(
    [property: Probability] double Chance = 1d,
    [property: Probability] double TargetMpDamageFactor = 0.5d,
    [property: Probability] double SourceHpBacklashFactor = 0.5d,
    [property: NonNegative] double BacklashTargetMpFactor = 2d);

internal sealed class DepleteMpOrBacklashBattleEffectHandler
    : CustomBattleEffectHandler<DepleteMpOrBacklashBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Execute(
        IHitConfirmedEffectContext context,
        DepleteMpOrBacklashBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            context.Source is null ||
            context.Target is null ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        if (context.Source.MaxMp >= context.Target.Mp)
        {
            hookContext.DamageMp(
                context.Target,
                (int)Math.Floor(context.Target.Mp * parameters.TargetMpDamageFactor),
                "化功大法");
            return;
        }

        var sourceHpBeforeBacklash = context.Source.Hp;
        hookContext.Damage(
            context.Source,
            (int)Math.Floor(sourceHpBeforeBacklash * parameters.SourceHpBacklashFactor),
            "化功反噬");
        hookContext.DamageMp(
            context.Target,
            (int)Math.Floor(sourceHpBeforeBacklash * parameters.BacklashTargetMpFactor),
            "化功大法·反噬");
    }
}
