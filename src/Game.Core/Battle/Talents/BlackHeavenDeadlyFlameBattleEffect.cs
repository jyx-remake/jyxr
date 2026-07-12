using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record BlackHeavenDeadlyFlameBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactor = 0.25d);

internal sealed class BlackHeavenDeadlyFlameBattleEffectHandler
    : CustomBattleEffectHandler<BlackHeavenDeadlyFlameBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        BlackHeavenDeadlyFlameBattleEffectParameters parameters)
    {
        var target = context.Target;
        if (target is null || context.Skill?.Power is not > 0)
        {
            return;
        }

        var additionalDamage = (int)(target.Hp * parameters.CurrentHpFactor);
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.PostAdd,
            additionalDamage);
    }
}
