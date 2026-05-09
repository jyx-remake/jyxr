using Game.Core.Model;

namespace Game.Core.Battle;

public sealed record BattleTurnPlan(
    string UnitId,
    GridPosition MoveDestination,
    BattleMainActionPlan MainAction);
