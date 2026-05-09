namespace Game.Core.Battle;

public sealed record BattleSkillAiEvaluation(
    int EnemyDamage,
    int AllyDamage,
    int EnemyKills,
    int AllyKills,
    int EnemyHitCount);
