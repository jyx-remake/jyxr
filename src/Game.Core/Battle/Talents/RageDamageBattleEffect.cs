using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RageDamageBattleEffectParameters(
    [property: NonNegative] double BonusPerRage,
    double? EnhancedBonusPerRage = null,
    string? AllyTalentId = null,
    string? AllyCharacterId = null);

internal sealed class RageDamageBattleEffectHandler
    : CustomBattleEffectHandler<RageDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(RageDamageBattleEffectParameters parameters)
    {
        var hasEnhancedBonus = parameters.EnhancedBonusPerRage is not null;
        var hasAllyTalent = !string.IsNullOrWhiteSpace(parameters.AllyTalentId);
        var hasAllyCharacter = !string.IsNullOrWhiteSpace(parameters.AllyCharacterId);
        if (parameters.EnhancedBonusPerRage is < 0d ||
            hasEnhancedBonus != hasAllyTalent ||
            hasEnhancedBonus != hasAllyCharacter)
        {
            throw new InvalidOperationException(
                "Enhanced rage damage requires a non-negative enhancedBonusPerRage, allyTalentId, and allyCharacterId together.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        RageDamageBattleEffectParameters parameters)
    {
        if (context.Source is null ||
            !string.Equals(context.Unit.Id, context.Source.Id, StringComparison.Ordinal))
        {
            return;
        }

        var bonusPerRage = parameters.BonusPerRage;
        if (parameters.EnhancedBonusPerRage is { } enhancedBonus &&
            context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Source.Team &&
                string.Equals(unit.Character.Definition.Id, parameters.AllyCharacterId, StringComparison.Ordinal) &&
                unit.Character.HasEffectiveTalent(parameters.AllyTalentId!)))
        {
            bonusPerRage = enhancedBonus;
        }

        TalentDamageModifier.MultiplyAttack(context, 1d + context.Source.Rage * bonusPerRage);
    }
}
