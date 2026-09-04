using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Applies a buff around the hit's source.  This is the engine counterpart of
/// the legacy battlefield-radius branches (for example 冰冻三尺); the talent
/// conditions still decide whose hook owns the effect and when it rolls.
/// </summary>
public sealed record AreaBuffOnHitBattleEffectParameters(
    [property: Required, MinLength(1)] string BuffId,
    [property: NonNegative] int Level,
    [property: Positive] int Duration,
    [property: NonNegative] int Radius,
    bool SkipTargetsWithBuff = true);

internal sealed class AreaBuffOnHitBattleEffectHandler
    : CustomBattleEffectHandler<AreaBuffOnHitBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(AreaBuffOnHitBattleEffectParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.BuffId))
        {
            throw new InvalidOperationException("Area buff ID cannot be blank.");
        }
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        AreaBuffOnHitBattleEffectParameters parameters)
    {
        var anchor = context.Source;
        if (anchor is null)
        {
            return;
        }

        foreach (var target in context.State.GetLivingUnits()
                     .Where(unit => unit.Team != context.Unit.Team)
                     .Where(unit => unit.Position.ManhattanDistanceTo(anchor.Position) <= parameters.Radius)
                     .Where(unit => !parameters.SkipTargetsWithBuff || !unit.HasBuff(parameters.BuffId)))
        {
            context.ApplyBuff(target, parameters.BuffId, parameters.Level, parameters.Duration);
        }
    }
}
