using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Reduces final damage by a target-current-HP amount scaled by target level.
/// This is the supported engine form of the legacy 宗师/若水 branch.
/// </summary>
public sealed record TargetHpLevelDamageReductionBattleEffectParameters(
    [property: NonNegative] double ScalePerUnitLevel,
    [property: Probability] double Chance = 1d);

internal sealed class TargetHpLevelDamageReductionBattleEffectHandler
    : CustomBattleEffectHandler<TargetHpLevelDamageReductionBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        TargetHpLevelDamageReductionBattleEffectParameters parameters)
    {
        if (context.Target is null ||
            context.Source is null ||
            context.Target.Team == context.Source.Team ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var reduction = Math.Floor(
            context.Target.Hp * context.Target.Character.Level * parameters.ScalePerUnitLevel);
        if (reduction > 0d)
        {
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.FinalDamage,
                ModifierOp.PostAdd,
                -reduction);
        }
    }
}
