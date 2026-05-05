using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class LocationState
{
    private readonly Dictionary<string, MapPosition> _largeMapPositions = new(StringComparer.Ordinal);

    public string CurrentMapId { get; private set; } = "";

    public IReadOnlyDictionary<string, MapPosition> LargeMapPositions => _largeMapPositions;

    public static LocationState Restore(LocationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var location = new LocationState
        {
            CurrentMapId = record.CurrentMapId ?? "",
        };

        foreach (var entry in record.LargeMapPositions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Key);
            location._largeMapPositions.Add(entry.Key, entry.Value);
        }

        return location;
    }

    // TODO 加参数？
    public void ChangeMap(string mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        CurrentMapId = mapId;
    }

    public void SetLargeMapPosition(string mapId, MapPosition position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        _largeMapPositions[mapId] = position;
    }

    public MapPosition GetLargeMapPosition(string mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        return _largeMapPositions.GetValueOrDefault(mapId, MapPosition.Zero);
    }

    public bool TryGetLargeMapPosition(string mapId, out MapPosition position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        return _largeMapPositions.TryGetValue(mapId, out position);
    }

    public LocationRecord ToRecord() => new(CurrentMapId, new Dictionary<string, MapPosition>(_largeMapPositions, StringComparer.Ordinal));
}
