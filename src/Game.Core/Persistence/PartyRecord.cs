namespace Game.Core.Persistence;

public sealed record PartyRecord(
    IReadOnlyList<string> MemberIds,
    IReadOnlyList<string> FollowerIds,
    IReadOnlyList<string> ReserveIds);
