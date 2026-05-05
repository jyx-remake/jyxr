using System.Diagnostics;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Definitions.Skills;

public static class SkillHelper
{
    public static SkillImpactType GetImpactType(SkillInstance skill) => GetImpactType(skill.WeaponType);
    public static SkillImpactType GetImpactType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Quanzhang => SkillImpactType.Single,
            WeaponType.Jianfa => SkillImpactType.Line,
            WeaponType.Daofa => SkillImpactType.Cleave,
            WeaponType.Qimen => SkillImpactType.Plus,
            _ => SkillImpactType.Single
        };
    }

    public static int GetCastSize(SkillInstance skill) => GetCastSize(skill.ImpactType);
    public static int GetCastSize(SkillImpactType impactType) => impactType switch
    {
        SkillImpactType.Single => 3,
        SkillImpactType.Plus => 0,
        SkillImpactType.Star => 0,
        SkillImpactType.Line => 1,
        SkillImpactType.Square => 1,
        SkillImpactType.Fan => 1,
        SkillImpactType.Ring => 0,
        SkillImpactType.X => 0,
        SkillImpactType.Cleave => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(impactType), impactType, null),
    };
    public static int GetImpactSize(SkillInstance skill) => GetImpactSize(skill.ImpactType);
    public static int GetImpactSize(SkillImpactType impactType) => impactType switch
    {
        SkillImpactType.Single => 1,
        SkillImpactType.Plus => 2,
        SkillImpactType.Star => 2,
        SkillImpactType.Line => 4,
        SkillImpactType.Square => 4,
        SkillImpactType.Fan => 3,
        SkillImpactType.Ring => 3,
        SkillImpactType.X => 4,
        SkillImpactType.Cleave => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(impactType), impactType, null),
    };

    public static int GetMpCost(SkillInstance skill)
    {
        var baseCost = 8 * (int)skill.Power;
        return skill.ImpactType switch
        { 
            SkillImpactType.Single => baseCost * 2,
            SkillImpactType.Plus => baseCost * skill.ImpactSize,
            SkillImpactType.Star => (int)(baseCost * skill.ImpactSize * 1.3),
            SkillImpactType.Line => (int)(baseCost * skill.ImpactSize * 0.45),
            SkillImpactType.Square => (int)(baseCost * skill.ImpactSize * 3),
            SkillImpactType.Fan => (int)(baseCost * skill.ImpactSize * 2.5),
            SkillImpactType.Cleave => baseCost * 2,
            _ => (int)(baseCost * skill.ImpactSize * 1.5)
        };
    }
}
