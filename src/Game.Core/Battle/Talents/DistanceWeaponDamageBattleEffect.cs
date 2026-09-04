using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Applies the legacy distance profile used by 绝命杀手 to knife skills.
/// Distance is Chebyshev distance, matching the Lua branch.
/// </summary>
public sealed record DistanceWeaponDamageBattleEffectParameters(
    WeaponType WeaponType = WeaponType.Daofa,
    [property: NonNegative] double AdjacentAttackMultiplier = 2d,
    [property: NonNegative] double AdjacentCriticalMultiplier = 2d,
    [property: NonNegative] double DistantPenaltyPerCell = 0.1d);

internal sealed class DistanceWeaponDamageBattleEffectHandler
    : CustomBattleEffectHandler<DistanceWeaponDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        DistanceWeaponDamageBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !ReferenceEquals(context.Target, context.Unit) ||
            context.Skill?.Power is not > 0 ||
            context.Skill.WeaponType != parameters.WeaponType)
        {
            return;
        }

        var distance = Math.Max(
            Math.Abs(context.Source.Position.X - context.Target.Position.X),
            Math.Abs(context.Source.Position.Y - context.Target.Position.Y));
        if (distance == 1)
        {
            AddAttackMultiplier(context, parameters.AdjacentAttackMultiplier);
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.CriticalChance,
                ModifierOp.More,
                parameters.AdjacentCriticalMultiplier);
        }
        else if (distance > 2)
        {
            AddAttackMultiplier(
                context,
                Math.Max(0d, 1d - parameters.DistantPenaltyPerCell * (distance - 2)));
        }
    }

    private static void AddAttackMultiplier(
        IDamageCalculationEffectContext context,
        double factor)
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
