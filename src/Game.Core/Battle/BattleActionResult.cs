using Game.Core.Model;

namespace Game.Core.Battle;

public sealed record BattleActionResult(
    bool Success,
    string Message,
    IReadOnlyList<string> AffectedUnitIds,
    IReadOnlyList<BattleEvent> Events,
    IReadOnlyList<GridPosition> ImpactedPositions)
{
    public static BattleActionResult Succeeded(
        string message,
        IReadOnlyList<string>? affectedUnitIds = null,
        IReadOnlyList<BattleEvent>? events = null,
        IReadOnlyList<GridPosition>? impactedPositions = null) =>
        new(true, message, affectedUnitIds ?? [], events ?? [], impactedPositions ?? []);

    public static BattleActionResult Failed(string message) =>
        new(false, message, [], [], []);
}
