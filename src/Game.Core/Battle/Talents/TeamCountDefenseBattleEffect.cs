using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record TeamCountDefenseBattleEffectParameters(
    [property: Positive] int MinimumTeamSize = 6,
    [property: Probability] double Chance = 0.5d,
    [property: Positive] double DefenseMultiplier = 2d,
    [property: NonNegative] double DefenseBonusPerUnit = 0d,
    [property: NotWhiteSpace] string TalentId = "六合阵");

internal sealed class TeamCountDefenseBattleEffectHandler
    : CustomBattleEffectHandler<TeamCountDefenseBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        TeamCountDefenseBattleEffectParameters parameters)
    {
        if (context.Target is null ||
            !string.Equals(context.Unit.Id, context.Target.Id, StringComparison.Ordinal) ||
            !context.Unit.Character.HasEffectiveTalent(parameters.TalentId) ||
            context.Skill?.Power is not > 0)
        {
            return;
        }

        if (context.State.GetLivingUnits().Count(unit => unit.Team == context.Unit.Team) <
                parameters.MinimumTeamSize ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        if (parameters.DefenseBonusPerUnit > 0d)
        {
            var teamSize = context.State.GetLivingUnits().Count(unit => unit.Team == context.Unit.Team);
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.TargetDefence,
                ModifierOp.Add,
                teamSize * parameters.DefenseBonusPerUnit);
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.TargetDefence,
            ModifierOp.More,
            parameters.DefenseMultiplier);
    }
}
