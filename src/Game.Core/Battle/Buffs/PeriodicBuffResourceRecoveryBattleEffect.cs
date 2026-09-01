using Game.Core.Affix;

namespace Game.Core.Battle.Buffs;

public sealed record PeriodicBuffResourceRecoveryBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactorPerBuffLevel = 0d,
    [property: NonNegative] double MaximumHpFactorPerBuffLevel = 0d,
    [property: NonNegative] double CurrentMpFactorPerBuffLevel = 0d,
    [property: NonNegative] double MaximumMpFactorPerBuffLevel = 0d,
    string? Detail = null);

internal sealed class PeriodicBuffResourceRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<PeriodicBuffResourceRecoveryBattleEffectParameters, IPeriodicBuffEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.AfterBuffRound };

    public override void Execute(
        IPeriodicBuffEffectContext context,
        PeriodicBuffResourceRecoveryBattleEffectParameters parameters)
    {
        var level = context.Buff?.Level ?? 0;
        if (level <= 0)
        {
            return;
        }

        var hp = (int)Math.Ceiling(level * (
            context.Unit.Hp * parameters.CurrentHpFactorPerBuffLevel +
            context.Unit.MaxHp * parameters.MaximumHpFactorPerBuffLevel));
        var mp = (int)Math.Ceiling(level * (
            context.Unit.Mp * parameters.CurrentMpFactorPerBuffLevel +
            context.Unit.MaxMp * parameters.MaximumMpFactorPerBuffLevel));

        if (hp > 0)
        {
            context.ApplyHpRecovery(context.Unit, hp, parameters.Detail);
        }

        if (mp > 0)
        {
            context.ApplyMpRecovery(context.Unit, mp, parameters.Detail);
        }
    }
}

public sealed record PeriodicBuffDamageBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactor = 0d,
    [property: NonNegative] double MaximumHpFactor = 0d,
    bool MultiplyByBuffLevel = true,
    bool MultiplyByRemainingTurns = false,
    [property: NonNegative] int MinimumRemainingHp = 1,
    string? Detail = null);

internal sealed class PeriodicBuffDamageBattleEffectHandler
    : CustomBattleEffectHandler<PeriodicBuffDamageBattleEffectParameters, IPeriodicBuffEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.AfterBuffRound };

    public override void Execute(
        IPeriodicBuffEffectContext context,
        PeriodicBuffDamageBattleEffectParameters parameters)
    {
        var multiplier = parameters.MultiplyByBuffLevel
            ? context.Buff?.Level ?? 0
            : 1;
        if (parameters.MultiplyByRemainingTurns)
        {
            multiplier *= context.Buff?.RemainingTurns ?? 0;
        }

        var requested = (int)Math.Ceiling(multiplier * (
            context.Unit.Hp * parameters.CurrentHpFactor +
            context.Unit.MaxHp * parameters.MaximumHpFactor));
        var amount = Math.Min(
            requested,
            Math.Max(0, context.Unit.Hp - parameters.MinimumRemainingHp));
        if (amount > 0)
        {
            context.ApplyDirectDamage(context.Unit, amount, parameters.Detail);
        }
    }
}
