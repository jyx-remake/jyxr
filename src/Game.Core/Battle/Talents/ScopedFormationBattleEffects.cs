using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record TeamCountAttackBonusParameters(
    [property: NonNegative] double FactorPerUnit,
    [property: NonNegative] int MaximumUnits = 10,
    bool IncludeSelf = false,
    bool CountTargetTeam = false);

public sealed class TeamCountAttackBonusHandler
    : CustomBattleEffectHandler<TeamCountAttackBonusParameters, IDamageCalculationEffectContext>
{
    public override bool SupportsPreview => true;
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(IDamageCalculationEffectContext context, TeamCountAttackBonusParameters parameters)
    {
        if (!ReferenceEquals(context.Source, context.Unit) || context.Skill?.Power is not > 0) return;
        var countedTeam = parameters.CountTargetTeam && context.Target is not null
            ? context.Target.Team
            : context.Unit.Team;
        var livingTeamSize = context.State.GetLivingUnits().Count(unit => unit.Team == countedTeam);
        var count = Math.Min(
            parameters.MaximumUnits,
            livingTeamSize - (parameters.IncludeSelf ? 0 : 1));
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.More,
            1d + parameters.FactorPerUnit * count);
    }
}
