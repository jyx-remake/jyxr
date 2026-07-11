using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ZhenwuFormationAttackBattleEffectParameters(
    [property: NotWhiteSpace] string FormationTalentId,
    [property: NonNegative] double MaxAttackIncrease = 0.1d,
    string? FloatText = null);

public sealed class ZhenwuFormationAttackBattleEffectHandler
    : CustomBattleEffectHandler<ZhenwuFormationAttackBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        ZhenwuFormationAttackBattleEffectParameters parameters)
    {
        var calculation = context.DamageCalculation;
        if (context.Skill?.Power is not > 0)
        {
            return;
        }

        foreach (var supporter in ZhenwuFormationMembers.GetOthers(context, parameters.FormationTalentId))
        {
            var increase = context.Random.NextDouble() * parameters.MaxAttackIncrease;
            calculation.AddModifier(
                BattleDamageContextField.FinalDamage,
                ModifierOp.More,
                1d + increase);

            if (!string.IsNullOrWhiteSpace(parameters.FloatText))
            {
                context.RequestFloatText(
                    supporter,
                    parameters.FloatText,
                    BattleFloatTextStyle.Beneficial);
            }
        }
    }
}

public sealed record ZhenwuFormationInterceptBattleEffectParameters(
    [property: NotWhiteSpace] string FormationTalentId,
    [property: Probability] double InterceptChance = 0.5d,
    [property: Probability] double DamageFactor = 0.3d,
    string? Speech = null);

public sealed class ZhenwuFormationInterceptBattleEffectHandler
    : CustomBattleEffectHandler<ZhenwuFormationInterceptBattleEffectParameters, IDamageApplicationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Execute(
        IDamageApplicationEffectContext context,
        ZhenwuFormationInterceptBattleEffectParameters parameters)
    {
        if (context.DamageAmount <= 0)
        {
            return;
        }

        foreach (var defender in ZhenwuFormationMembers.GetOthers(context, parameters.FormationTalentId))
        {
            if (!Probability.RollChance(context.Random, parameters.InterceptChance))
            {
                continue;
            }

            context.RedirectDamage(defender, parameters.DamageFactor);
            if (!string.IsNullOrWhiteSpace(parameters.Speech))
            {
                context.RequestSpeech(defender, parameters.Speech);
            }

            return;
        }
    }
}

internal static class ZhenwuFormationMembers
{
    public static IEnumerable<BattleUnit> GetOthers(
        IBattleEffectContext context,
        string formationTalentId) =>
        context.State.GetLivingUnits().Where(unit =>
            unit.Team == context.Unit.Team &&
            !string.Equals(
                unit.Character.Definition.Id,
                context.Unit.Character.Definition.Id,
                StringComparison.Ordinal) &&
            unit.Character.HasEffectiveTalent(formationTalentId));
}
