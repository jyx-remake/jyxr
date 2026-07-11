using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

internal static class TalentDamageModifier
{
    public static void MultiplyAttack(IDamageCalculationEffectContext context, double factor)
    {
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttackLow,
            ModifierOp.More,
            factor);
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttackHigh,
            ModifierOp.More,
            factor);
    }
}
