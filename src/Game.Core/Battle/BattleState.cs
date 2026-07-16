using Game.Core.Model;

namespace Game.Core.Battle;

public sealed class BattleState
{
    private readonly List<BattleUnit> _units;
    private List<BattleMessage>? _commandMessages;
    private int _commandDepth;
    private int _effectDepth;
    private long _effectSequence;

    public BattleState(
        BattleGrid grid,
        IEnumerable<BattleUnit> units,
        BattleRuleSettings? ruleSettings = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(units);

        Grid = grid;
        RuleSettings = ruleSettings ?? BattleRuleSettings.Neutral;
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

    public BattleRuleSettings RuleSettings { get; }

    public IReadOnlyList<BattleUnit> Units => _units;

    public BattleActionContext? CurrentAction { get; internal set; }

    public long Tick { get; internal set; }

    public long ActionSerial { get; internal set; }

    public BattleExecutionScope? CurrentExecutionScope { get; private set; }

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

    public void AddUnit(BattleUnit unit)
    {
        _units.Add(unit);
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

    internal void AddMessage(BattleMessage message)
    {
        if (_commandMessages is null)
        {
            throw new InvalidOperationException("Battle messages can only be emitted while a command is executing.");
        }

        _commandMessages.Add(message);
    }

    internal BattleCommandScope BeginCommand()
    {
        _commandMessages ??= [];
        _commandDepth++;
        return new BattleCommandScope(this, _commandMessages.Count);
    }

    internal sealed class BattleCommandScope(BattleState state, int startIndex) : IDisposable
    {
        private bool _disposed;

        public IReadOnlyList<BattleMessage> Messages =>
            state._commandMessages?.Skip(startIndex).ToArray() ?? [];

        public void Dispose()
        {
            if (_disposed) return;
            state._commandDepth--;
            if (state._commandDepth == 0) state._commandMessages = null;
            _disposed = true;
        }
    }

    internal IDisposable EnterEffect(string ruleSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleSource);
        const int maxEffectDepth = 64;
        if (_effectDepth >= maxEffectDepth)
        {
            throw new InvalidOperationException($"Battle effect chain exceeded maximum depth '{maxEffectDepth}'.");
        }

        var parent = CurrentExecutionScope;
        _effectDepth++;
        CurrentExecutionScope = new BattleExecutionScope(
            ++_effectSequence,
            parent?.Sequence,
            _effectDepth,
            ruleSource);
        return new EffectScope(this, parent);
    }

    private sealed class EffectScope(BattleState state, BattleExecutionScope? parent) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            state._effectDepth--;
            state.CurrentExecutionScope = parent;
            _disposed = true;
        }
    }


}
