using Game.Core.Affix;

namespace Game.Core.Battle;

internal sealed record BattleRecoveryResult(
    int RequestedAmount,
    int ResolvedAmount,
    int ActualAmount);

internal sealed class BattleRecoveryResolver(BattleHookTrigger triggerHooks)
{
    public BattleRecoveryResult Apply(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        BattleRecoveryKind kind,
        int amount)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var context = triggerHooks(
            state,
            HookTiming.BeforeRecoveryResolved,
            target,
            hookContext =>
            {
                hookContext.Source = source;
                hookContext.Target = target;
                hookContext.RecoveryKind = kind;
                hookContext.RecoveryAmount = amount;
            });
        var resolvedAmount = Math.Max(0, context.RecoveryAmount ?? amount);
        var actualAmount = kind switch
        {
            BattleRecoveryKind.Hp => target.RestoreHp(resolvedAmount),
            BattleRecoveryKind.Mp => target.RestoreMp(resolvedAmount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        return new BattleRecoveryResult(amount, resolvedAmount, actualAmount);
    }
}
