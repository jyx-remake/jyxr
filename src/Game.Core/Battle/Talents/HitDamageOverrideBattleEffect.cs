using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Rewrites the already-calculated hit damage using the target's current HP.
/// IHitResultEffectContext is the engine's supported mutation point for the
/// legacy direct-kill/current-HP branches.
/// </summary>
public sealed record HitDamageOverrideBattleEffectParameters(
    [property: Probability] double Chance = 1d,
    double? TargetCurrentHpFactor = null,
    double? TargetCurrentHpAdditiveFactor = null,
    int? RemainingHp = null,
    bool RequireSourceHpAtLeastTarget = false);

internal sealed class HitDamageOverrideBattleEffectHandler
    : CustomBattleEffectHandler<HitDamageOverrideBattleEffectParameters, IHitResultEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeHitResolved };

    public override void Validate(HitDamageOverrideBattleEffectParameters parameters)
    {
        var modeCount = (parameters.TargetCurrentHpFactor is not null ? 1 : 0) +
            (parameters.TargetCurrentHpAdditiveFactor is not null ? 1 : 0) +
            (parameters.RemainingHp is not null ? 1 : 0);
        if (modeCount != 1)
        {
            throw new InvalidOperationException(
                "Hit damage override requires exactly one HP override mode.");
        }
    }

    public override void Execute(
        IHitResultEffectContext context,
        HitDamageOverrideBattleEffectParameters parameters)
    {
        if (context.Target is null ||
            !Probability.RollChance(context.Random, parameters.Chance) ||
            (parameters.RequireSourceHpAtLeastTarget &&
             (context.Source is null || context.Source.Hp < context.Target.Hp)))
        {
            return;
        }

        var damage = parameters.RemainingHp is { } remainingHp
            ? Math.Max(0, context.Target.Hp - remainingHp)
            : parameters.TargetCurrentHpFactor is { } currentHpFactor
                ? (int)Math.Ceiling(context.Target.Hp * currentHpFactor)
                : (int)Math.Ceiling(context.Target.Hp * parameters.TargetCurrentHpAdditiveFactor!.Value) + context.DamageAmount;
        context.DamageAmount = parameters.TargetCurrentHpAdditiveFactor is not null
            ? damage
            : Math.Max(context.DamageAmount, damage);
    }
}
