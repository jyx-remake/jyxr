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

public enum BattleSkillDisabledReason
{
    None,
    Explicit,
    Seal,
}

public sealed record BattleSkillAvailability(
    SkillInstance Skill,
    int MpCost,
    BattleSkillAvailabilityStatus Status,
    int RemainingCooldown = 0,
    BattleSkillDisabledReason DisabledReason = BattleSkillDisabledReason.None)
{
    public bool IsAvailable => Status == BattleSkillAvailabilityStatus.Available;

    public bool IsSealed => Status == BattleSkillAvailabilityStatus.Disabled &&
        DisabledReason == BattleSkillDisabledReason.Seal;
}
