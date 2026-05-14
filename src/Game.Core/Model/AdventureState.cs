using System.Text.Json.Serialization;
using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class AdventureState
{
    public const string DefaultFavorabilityTargetId = "女主";
    private const int DefaultFavorability = 50;

    private readonly Dictionary<string, int> _favorabilityByTarget = new(StringComparer.Ordinal);

    public int Round { get; private set; } = 1;

    public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Normal;

    public string? SectId { get; private set; }

    public int Morality { get; private set; } = 50;

    public int Favorability => GetFavorability();

    public IReadOnlyDictionary<string, int> FavorabilityByTarget => _favorabilityByTarget;

    public double Rank { get; private set; }

    public static AdventureState Restore(AdventureStateRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Round, 1);
        if (!Enum.IsDefined(record.Difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(record));
        }

        var adventure = new AdventureState
        {
            Round = record.Round,
            Difficulty = record.Difficulty,
            SectId = string.IsNullOrWhiteSpace(record.SectId) ? null : record.SectId,
            Morality = record.Morality,
            Rank = record.Rank,
        };

        foreach (var (targetId, value) in record.FavorabilityByTarget ?? new Dictionary<string, int>())
        {
            adventure.SetFavorability(targetId, value);
        }

        return adventure;
    }

    public void SetRound(int round)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        Round = round;
    }

    public void SetDifficulty(GameDifficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        Difficulty = difficulty;
    }

    public void SetSect(string? sectId) =>
        SectId = string.IsNullOrWhiteSpace(sectId)
            ? null
            : sectId;

    public void ChangeMorality(int delta) => Morality += delta;

    public int GetFavorability(string targetId = DefaultFavorabilityTargetId)
    {
        targetId = NormalizeFavorabilityTarget(targetId);
        return _favorabilityByTarget.GetValueOrDefault(targetId, DefaultFavorability);
    }

    public void ChangeFavorability(int delta) => ChangeFavorability(DefaultFavorabilityTargetId, delta);

    public void ChangeFavorability(string targetId, int delta)
    {
        targetId = NormalizeFavorabilityTarget(targetId);
        _favorabilityByTarget[targetId] = checked(GetFavorability(targetId) + delta);
    }

    private void SetFavorability(string targetId, int value)
    {
        targetId = NormalizeFavorabilityTarget(targetId);
        _favorabilityByTarget[targetId] = value;
    }

    public void SetRank(double value) => Rank = value;

    public bool IsDifficulty(string modeId) =>
        string.Equals(GetModeId(), modeId, StringComparison.Ordinal);

    public bool IsInSect(string sectId) =>
        !string.IsNullOrWhiteSpace(SectId) &&
        string.Equals(SectId, sectId, StringComparison.Ordinal);

    public string GetModeId() => Difficulty switch
    {
        GameDifficulty.Normal => "normal",
        GameDifficulty.Hard => "hard",
        GameDifficulty.Crazy => "crazy",
        _ => throw new InvalidOperationException($"Unsupported difficulty: {Difficulty}"),
    };

    public AdventureStateRecord ToRecord() =>
        new(
            Round,
            Difficulty,
            SectId,
            Morality,
            _favorabilityByTarget
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal),
            Rank);

    private static string NormalizeFavorabilityTarget(string targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        return targetId;
    }
}

public enum GameDifficulty
{
    [JsonStringEnumMemberName("normal")]
    Normal,
    [JsonStringEnumMemberName("hard")]
    Hard,
    [JsonStringEnumMemberName("crazy")]
    Crazy,
}
