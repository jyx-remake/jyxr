using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record LocationRecord(
    string CurrentMapId,
    IReadOnlyDictionary<string, MapPosition> LargeMapPositions);
