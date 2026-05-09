namespace Game.Core.Battle;

public sealed record BattleTurnCandidate(
    BattleTurnPlan Plan,
    double Score,
    int EnemyDamage,
    int AllyDamage,
    int EnemyKills,
    int AllyKills,
    int EnemyHitCount,
    int DistanceToNearestEnemy);
