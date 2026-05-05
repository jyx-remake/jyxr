using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record AdventureStateRecord(
    int Round,
    GameDifficulty Difficulty,
    string? SectId,
    int Morality,
    int Favorability,
    double Rank);
