using Game.Core.Model;

namespace Game.Core.Battle;

public sealed record BattleCommandResult<T>(
    bool Success,
    string Message,
    T? Value,
    IReadOnlyList<BattleMessage> Messages)
{
    public static BattleCommandResult<T> Succeeded(T value, IReadOnlyList<BattleMessage> messages, string message = "") =>
        new(true, message, value, messages);

    public static BattleCommandResult<T> Failed(string message, IReadOnlyList<BattleMessage>? messages = null) =>
        new(false, message, default, messages ?? []);
}

public sealed record BattleActionResult(
    IReadOnlyList<string> AffectedUnitIds,
    IReadOnlyList<GridPosition> ImpactedPositions,
    BattleSkillCastInfo? SkillCast = null);
