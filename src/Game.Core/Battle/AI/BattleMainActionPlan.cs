using Game.Core.Model;

namespace Game.Core.Battle;

public enum BattleMainActionKind
{
    CastSkill,
    Rest,
}

public sealed record BattleMainActionPlan(
    BattleMainActionKind Kind,
    string? SkillId = null,
    GridPosition? TargetPosition = null)
{
    public static BattleMainActionPlan CastSkill(string skillId, GridPosition targetPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        return new BattleMainActionPlan(BattleMainActionKind.CastSkill, skillId, targetPosition);
    }

    public static BattleMainActionPlan Rest() => new(BattleMainActionKind.Rest);
}
