using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record CompanionDodgeBattleEffectParameters(
    [property: NotWhiteSpace] string CompanionName,
    [property: Probability] double Chance);

internal sealed class CompanionDodgeBattleEffectHandler
    : CustomBattleEffectHandler<CompanionDodgeBattleEffectParameters, IHitResultEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeHitResolved };

    public override void Execute(
        IHitResultEffectContext context,
        CompanionDodgeBattleEffectParameters parameters)
    {
        if (!context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Unit.Team &&
                string.Equals(unit.Character.Definition.Name, parameters.CompanionName, StringComparison.Ordinal)) ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.HitState = BattleHitState.Miss;
        context.SuppressHitEffects = true;
    }
}
