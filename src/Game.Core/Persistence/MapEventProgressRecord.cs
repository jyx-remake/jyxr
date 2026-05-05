namespace Game.Core.Persistence;

public sealed record MapEventProgressRecord(
    IReadOnlyList<string> CompletedEventIds);
