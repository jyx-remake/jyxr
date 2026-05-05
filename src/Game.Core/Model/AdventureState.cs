using System.Text.Json.Serialization;
using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class AdventureState
{
    public int Round { get; private set; } = 1;

    public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Normal;

    public string? SectId { get; private set; }

    public int Morality { get; private set; } = 50;

    public int Favorability { get; private set; } = 50;

    public double Rank { get; private set; }

    public static AdventureState Restore(AdventureStateRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Round, 1);
        if (!Enum.IsDefined(record.Difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(record));
        }

        return new AdventureState
        {
            Round = record.Round,
            Difficulty = record.Difficulty,
            SectId = string.IsNullOrWhiteSpace(record.SectId) ? null : record.SectId,
            Morality = record.Morality,
            Favorability = record.Favorability,
            Rank = record.Rank,
        };
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

    public void ChangeFavorability(int delta) => Favorability += delta;

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
        new(Round, Difficulty, SectId, Morality, Favorability, Rank);
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
