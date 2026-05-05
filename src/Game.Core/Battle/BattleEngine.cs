using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    internal const int TimelineTicksPerRound = BattleBuffInstance.TimelineTicksPerRound;
    private const double ActionGaugeThreshold = 100d;

    private readonly BattleDamageCalculator _damageCalculator;
    private readonly BattleHookExecutor _hookExecutor;
    private readonly IRandomService _random;
    private readonly Func<string, BuffDefinition> _buffResolver;

    public BattleEngine(
        BattleDamageCalculator? damageCalculator = null,
        BattleHookExecutor? hookExecutor = null,
        IRandomService? random = null,
        Func<string, BuffDefinition>? buffResolver = null)
    {
        _damageCalculator = damageCalculator ?? new BattleDamageCalculator();
        _hookExecutor = hookExecutor ?? new BattleHookExecutor();
        _random = random ?? SharedRandomService.Instance;
        _buffResolver = buffResolver ?? MissingBuffResolver;
    }

    private static BattleActionResult ValidateActingUnit(
        BattleState state,
        string unitId,
        bool requireMainActionAvailable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        if (state.CurrentAction is null)
        {
            return BattleActionResult.Failed("No unit is acting.");
        }

        if (!string.Equals(state.CurrentAction.ActingUnitId, unitId, StringComparison.Ordinal))
        {
            return BattleActionResult.Failed("It is not this unit's action.");
        }

        if (requireMainActionAvailable && state.CurrentAction.HasCommittedMainAction)
        {
            return BattleActionResult.Failed("Main action has already been committed.");
        }

        return BattleActionResult.Succeeded("Valid.");
    }

    private void EndActionCore(BattleState state, BattleUnit unit, bool committedMainAction)
    {
        var context = state.CurrentAction;
        if (context is null || context.ActingUnitId != unit.Id)
        {
            throw new InvalidOperationException($"Unit '{unit.Id}' is not acting.");
        }

        context.HasCommittedMainAction = committedMainAction;
        TriggerHooks(state, HookTiming.AfterActionEnd, unit);
        unit.ActionGauge = Math.Max(0, unit.ActionGauge - ActionGaugeThreshold);
        state.CurrentAction = null;

        AddEvent(state, new BattleEvent(BattleEventKind.ActionEnded, unit.Id));
    }

    private static void UpdateFacingByMovement(BattleUnit unit, IReadOnlyList<GridPosition> path)
    {
        if (path.Count == 0)
        {
            return;
        }

        UpdateFacingByTarget(unit, path[^1]);
    }

    private static void UpdateFacingByTarget(BattleUnit unit, GridPosition target)
    {
        if (target.X < unit.Position.X)
        {
            unit.Facing = BattleFacing.Left;
        }
        else if (target.X > unit.Position.X)
        {
            unit.Facing = BattleFacing.Right;
        }
    }

    private static void AddEvent(BattleState state, BattleEvent battleEvent) => state.AddEvent(battleEvent);

    private sealed class SharedRandomService : IRandomService
    {
        public static SharedRandomService Instance { get; } = new();

        public double NextDouble() => Random.Shared.NextDouble();

        public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
    }

    private static BuffDefinition MissingBuffResolver(string buffId) =>
        throw new InvalidOperationException($"Battle engine cannot resolve buff '{buffId}'.");
}
