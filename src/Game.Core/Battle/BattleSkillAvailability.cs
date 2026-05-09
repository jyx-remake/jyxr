using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public enum BattleSkillAvailabilityStatus
{
    Available,
    Cooldown,
    Disabled,
    NotEnoughMp,
    NotEnoughRage,
}

public sealed record BattleSkillAvailability(
    SkillInstance Skill,
    int MpCost,
    BattleSkillAvailabilityStatus Status,
    int RemainingCooldown = 0)
{
    public bool IsAvailable => Status == BattleSkillAvailabilityStatus.Available;
}
