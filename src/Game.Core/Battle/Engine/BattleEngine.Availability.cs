using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public BattleSkillAvailability EvaluateSkillAvailability(BattleState state, string unitId, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        var unit = state.GetUnit(unitId);
        if (!ReferenceEquals(skill.Owner, unit.Character))
        {
            throw new InvalidOperationException("Skill does not belong to the specified battle unit.");
        }

        return EvaluateSkillAvailabilityCore(state, unit, skill);
    }

    private BattleSkillAvailability EvaluateSkillAvailabilityCore(BattleState state, BattleUnit unit, SkillInstance skill)
    {
        var mpCost = ResolveSkillMpCostPreview(state, unit, skill);
        if (skill.CurrentCooldown > 0)
        {
            return new BattleSkillAvailability(
                skill,
                mpCost,
                BattleSkillAvailabilityStatus.Cooldown,
                skill.CurrentCooldown);
        }

        if (unit.DisabledSkillIds.Contains(skill.Id))
        {
            return new BattleSkillAvailability(skill, mpCost, BattleSkillAvailabilityStatus.Disabled);
        }

        if (unit.Mp < mpCost)
        {
            return new BattleSkillAvailability(skill, mpCost, BattleSkillAvailabilityStatus.NotEnoughMp);
        }

        if (unit.Rage < skill.RageCost)
        {
            return new BattleSkillAvailability(skill, mpCost, BattleSkillAvailabilityStatus.NotEnoughRage);
        }

        return new BattleSkillAvailability(skill, mpCost, BattleSkillAvailabilityStatus.Available);
    }
}
