using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record CompanionCombatModifierBattleEffectParameters(
    [property: NotWhiteSpace] string BeneficiaryName,
    [property: NotWhiteSpace] string CompanionName,
    [property: NotWhiteSpace] string Field,
    [property: NonNegative] double Factor,
    [property: Probability] double Chance = 1d);

internal sealed class CompanionCombatModifierBattleEffectHandler
    : CustomBattleEffectHandler<CompanionCombatModifierBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(CompanionCombatModifierBattleEffectParameters parameters)
    {
        if (parameters.Field is not ("source_attack" or "target_defence"))
        {
            throw new InvalidOperationException(
                "Companion combat modifier field must be 'source_attack' or 'target_defence'.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        CompanionCombatModifierBattleEffectParameters parameters)
    {
        if (!string.Equals(context.Unit.Character.Definition.Name, parameters.BeneficiaryName, StringComparison.Ordinal) ||
            !context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Unit.Team &&
                string.Equals(unit.Character.Definition.Name, parameters.CompanionName, StringComparison.Ordinal)) ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            parameters.Field == "source_attack"
                ? BattleDamageContextField.SourceAttack
                : BattleDamageContextField.TargetDefence,
            ModifierOp.More,
            parameters.Factor);
    }
}
