using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed record BattleSkillAiContext(
    BattleState State,
    BattleUnit Source,
    SkillInstance Skill,
    GridPosition MoveDestination,
    GridPosition TargetPosition,
    IReadOnlyList<BattleUnit> Targets);
