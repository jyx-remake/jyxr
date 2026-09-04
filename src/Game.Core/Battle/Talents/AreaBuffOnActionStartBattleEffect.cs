using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record AreaBuffOnActionStartBattleEffectParameters(
    [property: NotWhiteSpace] string BuffId,
    [property: NonNegative] int Level,
    [property: Positive] int Duration,
    [property: NonNegative] int Radius,
    bool RequireHeroPresent = false);

internal sealed class AreaBuffOnActionStartBattleEffectHandler
    : CustomBattleEffectHandler<AreaBuffOnActionStartBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        AreaBuffOnActionStartBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            (parameters.RequireHeroPresent && !context.State.GetLivingUnits().Any(unit =>
                string.Equals(unit.Character.Id, "主角", StringComparison.Ordinal))))
        {
            return;
        }

        foreach (var target in context.State.GetLivingUnits()
                     .Where(unit => unit.Team == context.Unit.Team)
                     .Where(unit => unit.Position.ManhattanDistanceTo(context.Unit.Position) <= parameters.Radius))
        {
            hookContext.Engine.ApplyBuffByEffect(
                context.State,
                context.Unit,
                target,
                parameters.BuffId,
                parameters.Level,
                parameters.Duration,
                context.Timing);
        }
    }
}
