using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>Converts the attacker's missing HP into additive hit damage.</summary>
public sealed record MissingHpDamageBattleEffectParameters(
    [property: NonNegative] double ScalePerUnitLevel,
    [property: Probability] double Chance = 1d);

internal sealed class MissingHpDamageBattleEffectHandler
    : CustomBattleEffectHandler<MissingHpDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        MissingHpDamageBattleEffectParameters parameters)
    {
        if (context.Source is null ||
            !ReferenceEquals(context.Source, context.Unit) ||
            context.Skill?.Power is not > 0 ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var missingHp = Math.Max(0, context.Unit.MaxHp - context.Unit.Hp);
        var amount = Math.Floor(missingHp * context.Unit.Character.Level * parameters.ScalePerUnitLevel);
        if (amount > 0d)
        {
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.FinalDamage,
                ModifierOp.PostAdd,
                amount);
        }
    }
}
