using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record LowHealthTargetDamageBattleEffectParameters(
    [property: NonNegative] double MaximumBonus);

internal sealed class LowHealthTargetDamageBattleEffectHandler
    : CustomBattleEffectHandler<LowHealthTargetDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        LowHealthTargetDamageBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !string.Equals(context.Unit.Id, context.Source.Id, StringComparison.Ordinal))
        {
            return;
        }

        var hpRatio = context.Target.MaxHp <= 0
            ? 0d
            : Math.Clamp((double)context.Target.Hp / context.Target.MaxHp, 0d, 1d);
        TalentDamageModifier.MultiplyAttack(
            context,
            1d + parameters.MaximumBonus * (1d - hpRatio));
    }
}
