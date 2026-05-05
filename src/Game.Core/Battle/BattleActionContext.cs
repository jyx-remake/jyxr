using Game.Core.Model;

namespace Game.Core.Battle;

public sealed class BattleActionContext
{
    private readonly List<GridPosition> _movementTrace = [];
    private readonly HashSet<string> _spokenUnitIds = new(StringComparer.Ordinal);

    internal BattleActionContext(BattleUnit unit)
    {
        ActingUnitId = unit.Id;
        StartPosition = unit.Position;
        CurrentPosition = unit.Position;
        StartFacing = unit.Facing;
        RemainingMovePower = unit.GetMovePower();
    }

    public string ActingUnitId { get; }

    public GridPosition StartPosition { get; }

    public GridPosition CurrentPosition { get; internal set; }

    public BattleFacing StartFacing { get; }

    public int RemainingMovePower { get; internal set; }

    public bool HasMoved { get; internal set; }

    public bool HasCommittedMainAction { get; internal set; }

    public IReadOnlyList<GridPosition> MovementTrace => _movementTrace;

    internal void SetMovementTrace(IEnumerable<GridPosition> path)
    {
        _movementTrace.Clear();
        _movementTrace.AddRange(path);
    }

    internal void ClearMovementTrace() => _movementTrace.Clear();

    internal bool TryRegisterSpeech(string unitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
        return _spokenUnitIds.Add(unitId);
    }
}
