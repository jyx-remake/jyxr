using Game.Core.Model;

namespace Game.Core.Battle;

public sealed record BattleDamageRuleSettings
{
    public static BattleDamageRuleSettings Neutral { get; } = new();

    public GameDifficulty Difficulty { get; init; } = GameDifficulty.Normal;

    public int Round { get; init; } = 1;

    public int PlayerTeam { get; init; } = 1;

    public double RoundEnemyAttackAddRatio { get; init; }

    public double RoundEnemyDefenceAddRatio { get; init; }

    public bool EnableRoundEnemyAttackDefenceScaling { get; init; }

    public bool EnableDifficultyDamageScaling { get; init; }
}
