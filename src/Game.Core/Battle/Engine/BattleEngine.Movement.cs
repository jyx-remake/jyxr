using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public IReadOnlyDictionary<GridPosition, int> GetReachablePositions(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        var unit = state.GetUnit(unitId);
        var movePower = state.CurrentAction?.ActingUnitId == unit.Id
            ? state.CurrentAction.RemainingMovePower
            : unit.GetMovePower();

        return FindReachable(state, unit, unit.Position, movePower).Costs;
    }

    public BattleActionResult MoveTo(BattleState state, string unitId, GridPosition destination)
    {
        ArgumentNullException.ThrowIfNull(state);
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        var context = state.CurrentAction!;
        if (context.HasCommittedMainAction)
        {
            return BattleActionResult.Failed("Main action has already been committed.");
        }

        TriggerHooks(state, HookTiming.BeforeMove, unit);

        var reachable = FindReachable(state, unit, context.CurrentPosition, context.RemainingMovePower);
        if (!reachable.Costs.TryGetValue(destination, out var cost))
        {
            return BattleActionResult.Failed("Destination is not reachable.");
        }

        var path = RebuildPath(reachable.Previous, context.CurrentPosition, destination);
        context.RemainingMovePower -= cost;
        context.CurrentPosition = destination;
        context.HasMoved = context.HasMoved || destination != context.StartPosition;
        context.SetMovementTrace(path);
        UpdateFacingByMovement(unit, path);
        unit.Position = destination;

        var battleEvent = new BattleEvent(BattleEventKind.Moved, unit.Id, Detail: $"{destination.X},{destination.Y}");
        AddEvent(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterMove, unit);
        return BattleActionResult.Succeeded("Moved.", [unit.Id], [battleEvent]);
    }

    public BattleActionResult RollbackMove(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        var context = state.CurrentAction!;
        if (context.HasCommittedMainAction)
        {
            return BattleActionResult.Failed("Cannot rollback movement after main action.");
        }

        unit.Position = context.StartPosition;
        unit.Facing = context.StartFacing;
        context.CurrentPosition = context.StartPosition;
        context.RemainingMovePower = unit.GetMovePower();
        context.HasMoved = false;
        context.ClearMovementTrace();

        var battleEvent = new BattleEvent(BattleEventKind.MovementRolledBack, unit.Id);
        AddEvent(state, battleEvent);
        return BattleActionResult.Succeeded("Movement rolled back.", [unit.Id], [battleEvent]);
    }

    private static Reachability FindReachable(
        BattleState state,
        BattleUnit unit,
        GridPosition start,
        int movePower)
    {
        var costs = new Dictionary<GridPosition, int>
        {
            [start] = 0,
        };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new PriorityQueue<GridPosition, int>();
        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out var current, out var currentCost))
        {
            if (currentCost != costs[current])
            {
                continue;
            }

            foreach (var next in current.GetOrthogonalNeighbors())
            {
                if (!state.Grid.IsWalkable(next) || state.IsOccupied(next, unit.Id))
                {
                    continue;
                }

                var nextCost = currentCost + GetStepCost(state, unit, next);
                if (nextCost > movePower)
                {
                    continue;
                }

                if (costs.TryGetValue(next, out var knownCost) && knownCost <= nextCost)
                {
                    continue;
                }

                costs[next] = nextCost;
                previous[next] = current;
                queue.Enqueue(next, nextCost);
            }
        }

        return new Reachability(costs, previous);
    }

    private static int GetStepCost(BattleState state, BattleUnit unit, GridPosition destination)
    {
        var cost = 1;
        if (unit.HasTrait(TraitId.IgnoreZoneOfControl))
        {
            return cost;
        }

        var entersEnemyZone = destination.GetOrthogonalNeighbors()
            .Select(neighbor => state.GetUnitAt(neighbor, unit.Id))
            .Any(other => other is not null && state.AreEnemies(unit, other));
        return entersEnemyZone ? cost + 1 : cost;
    }

    private static IReadOnlyList<GridPosition> RebuildPath(
        IReadOnlyDictionary<GridPosition, GridPosition> previous,
        GridPosition start,
        GridPosition destination)
    {
        if (destination == start)
        {
            return [];
        }

        var path = new List<GridPosition>();
        var current = destination;
        path.Add(current);
        while (current != start)
        {
            current = previous[current];
            if (current != start)
            {
                path.Add(current);
            }
        }

        path.Reverse();
        return path;
    }

    private sealed record Reachability(
        IReadOnlyDictionary<GridPosition, int> Costs,
        IReadOnlyDictionary<GridPosition, GridPosition> Previous);
}
