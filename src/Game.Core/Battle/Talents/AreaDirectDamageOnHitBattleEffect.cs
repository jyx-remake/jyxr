using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Applies a direct-damage pulse to the source's enemy team after a confirmed
/// hit. The amount is derived from the confirmed hit amount, matching legacy
/// Lua branches that subtract a percentage of result.Hp from every enemy.
/// </summary>
public sealed record AreaDirectDamageOnHitBattleEffectParameters(
    [property: NonNegative] double DamageFactor);

internal sealed class AreaDirectDamageOnHitBattleEffectHandler
    : CustomBattleEffectHandler<AreaDirectDamageOnHitBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Execute(
        IHitConfirmedEffectContext context,
        AreaDirectDamageOnHitBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            context.Source is null ||
            hookContext.DamageAmount is not > 0)
        {
            return;
        }

        var amount = (int)Math.Floor(hookContext.DamageAmount.Value * parameters.DamageFactor);
        if (amount <= 0)
        {
            return;
        }

        foreach (var target in context.State.GetLivingUnits()
                     .Where(unit => unit.Team != context.Source.Team))
        {
            hookContext.Damage(target, amount, "范围直接伤害");
        }
    }
}
