using Game.Core.Model;

namespace Game.Application;

public static class GameDifficultyFormatter
{
    public static string FormatNameCn(GameDifficulty difficulty) => difficulty switch
    {
        GameDifficulty.Normal => "简单",
        GameDifficulty.Hard => "进阶",
        GameDifficulty.Crazy => "炼狱",
        _ => throw new InvalidOperationException($"Unsupported difficulty: {difficulty}"),
    };
}
