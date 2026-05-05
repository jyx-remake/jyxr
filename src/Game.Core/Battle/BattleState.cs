using Game.Core.Model;

namespace Game.Core.Battle;

public sealed class BattleState
{
    private readonly List<BattleUnit> _units;
    private readonly List<BattleEvent> _events = [];

    public BattleState(BattleGrid grid, IEnumerable<BattleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(units);

        Grid = grid;
        _units = units.ToList();
        if (_units.Count == 0)
        {
            throw new ArgumentException("Battle must contain at least one unit.", nameof(units));
        }

        foreach (var unit in _units)
        {
            if (!Grid.IsWalkable(unit.Position))
            {
                throw new InvalidOperationException($"Unit '{unit.Id}' starts on an invalid cell '{unit.Position}'.");
            }
        }

        var duplicatedPosition = _units
            .Where(static unit => unit.IsAlive)
            .GroupBy(static unit => unit.Position)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicatedPosition is not null)
        {
            throw new InvalidOperationException($"Multiple units start on cell '{duplicatedPosition.Key}'.");
        }
    }

    public BattleGrid Grid { get; }

    public IReadOnlyList<BattleUnit> Units => _units;

    public IReadOnlyList<BattleEvent> Events => _events;

    public BattleActionContext? CurrentAction { get; internal set; }

    public long Tick { get; internal set; }

    public long ActionSerial { get; internal set; }

    public BattleUnit GetUnit(string unitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
        return _units.First(unit => string.Equals(unit.Id, unitId, StringComparison.Ordinal));
    }

    public BattleUnit? TryGetUnit(string unitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
        return _units.FirstOrDefault(unit => string.Equals(unit.Id, unitId, StringComparison.Ordinal));
    }

    public BattleUnit? GetUnitAt(GridPosition position, string? ignoredUnitId = null) =>
        _units.FirstOrDefault(unit =>
            unit.IsAlive &&
            unit.Position == position &&
            !string.Equals(unit.Id, ignoredUnitId, StringComparison.Ordinal));

    public bool IsOccupied(GridPosition position, string? ignoredUnitId = null) =>
        GetUnitAt(position, ignoredUnitId) is not null;

    public bool AreEnemies(BattleUnit first, BattleUnit second) => first.Team != second.Team;

    public IReadOnlyList<BattleUnit> GetLivingUnits() =>
        _units.Where(static unit => unit.IsAlive).ToList();

    internal void AddEvent(BattleEvent battleEvent) => _events.Add(battleEvent);
}
