using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    internal bool TryRelocateByEffect(BattleState state, BattleUnit unit, GridPosition destination)
    {
        if (!unit.IsAlive || !state.Grid.IsWalkable(destination) || state.IsOccupied(destination, unit.Id))
        {
            return false;
        }

        unit.Position = destination;
        AddMessage(state, new BattleFact(BattleFactKind.Moved, unit.Id, detail: $"{destination.X},{destination.Y}"));
        return true;
    }

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

    public BattleCommandResult<BattleActionResult> MoveTo(BattleState state, string unitId, GridPosition destination)
    {
        ArgumentNullException.ThrowIfNull(state);
        using var command = state.BeginCommand();
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        var context = state.CurrentAction!;
        if (context.HasCommittedMainAction)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Main action has already been committed.", command.Messages);
        }

        TriggerHooks(state, HookTiming.BeforeMove, unit);

        var reachable = FindReachable(state, unit, context.CurrentPosition, context.RemainingMovePower);
        if (!reachable.Costs.TryGetValue(destination, out var cost))
        {
            return BattleCommandResult<BattleActionResult>.Failed("Destination is not reachable.", command.Messages);
        }

        var path = RebuildPath(reachable.Previous, context.CurrentPosition, destination);
        context.RemainingMovePower -= cost;
        context.CurrentPosition = destination;
        context.HasMoved = context.HasMoved || destination != context.StartPosition;
        context.SetMovementTrace(path);
        UpdateFacingByMovement(unit, path);
        unit.Position = destination;

        var battleEvent = new BattleFact(BattleFactKind.Moved, unit.Id, detail: $"{destination.X},{destination.Y}");
        AddMessage(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterMove, unit);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult([unit.Id], []), command.Messages, "Moved.");
    }

    public BattleCommandResult<BattleActionResult> RollbackMove(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        using var command = state.BeginCommand();
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        var context = state.CurrentAction!;
        if (context.HasCommittedMainAction)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Cannot rollback movement after main action.", command.Messages);
        }

        unit.Position = context.StartPosition;
        unit.Facing = context.StartFacing;
        context.CurrentPosition = context.StartPosition;
        context.RemainingMovePower = unit.GetMovePower();
        context.HasMoved = false;
        context.ClearMovementTrace();

        var battleEvent = new BattleFact(BattleFactKind.MovementRolledBack, unit.Id);
        AddMessage(state, battleEvent);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult([unit.Id], []), command.Messages, "Movement rolled back.");
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
