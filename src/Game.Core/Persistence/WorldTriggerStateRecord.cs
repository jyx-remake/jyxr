namespace Game.Core.Persistence;

public sealed record WorldTriggerStateRecord(
    bool IsBlocked,
    IReadOnlyList<string> CompletedTriggerIds);
