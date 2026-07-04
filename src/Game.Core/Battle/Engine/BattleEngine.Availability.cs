using Game.Core.Model;
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
        if (IsSealedByBuff(unit, skill))
        {
            return new BattleSkillAvailability(
                skill,
                mpCost,
                BattleSkillAvailabilityStatus.Disabled,
                DisabledReason: BattleSkillDisabledReason.Seal);
        }

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
            return new BattleSkillAvailability(
                skill,
                mpCost,
                BattleSkillAvailabilityStatus.Disabled,
                DisabledReason: BattleSkillDisabledReason.Explicit);
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

    private static bool IsSealedByBuff(BattleUnit unit, SkillInstance skill)
    {
        if (!IsExternalMartialSkill(skill))
        {
            return false;
        }

        if (unit.HasBuff(BattleContentIds.AllSkillSeal))
        {
            return true;
        }

        return skill.WeaponType switch
        {
            WeaponType.Quanzhang => unit.HasBuff(BattleContentIds.QuanzhangSeal),
            WeaponType.Jianfa => unit.HasBuff(BattleContentIds.JianfaSeal),
            WeaponType.Daofa => unit.HasBuff(BattleContentIds.DaofaSeal),
            WeaponType.Qimen => unit.HasBuff(BattleContentIds.QimenSeal),
            _ => false,
        };
    }

    private static bool IsExternalMartialSkill(SkillInstance skill) =>
        skill is ExternalSkillInstance ||
        skill is FormSkillInstance { Parent: ExternalSkillInstance };
}
