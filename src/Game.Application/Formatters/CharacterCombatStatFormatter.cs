using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Application.Formatters;

public static class CharacterCombatStatFormatter
{
    // Temporary legacy-style panel estimate. Replace this with the real damage
    // evaluation formula once the formal battlefield damage model is settled.
    public static CharacterCombatStatDisplay Calculate(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var attack = CalculateAttack(character);
        var defence = CalculateDefence(character);
        return new CharacterCombatStatDisplay(
            TruncateDisplayValue(attack),
            TruncateDisplayValue(defence),
            attack,
            defence);
    }

    public static string FormatAttack(CharacterInstance character) => Calculate(character).Attack.ToString();

    public static string FormatDefence(CharacterInstance character) => Calculate(character).Defence.ToString();

    private static double CalculateAttack(CharacterInstance character)
    {
        var equippedInternalSkill = GetEquippedInternalSkill(character);
        var maxSkillPower = GetMaxUsableSkillPower(character, equippedInternalSkill);
        if (maxSkillPower <= 0d)
        {
            return 0d;
        }

        var attackFactor = 1d;
        attackFactor *= 4d + character.GetStat(StatType.Bili) / 120d;
        attackFactor *= 2d + GetMaxWeaponStat(character) / 200d;
        attackFactor *= 1d + (equippedInternalSkill?.AttackRatio ?? 0d);
        attackFactor += character.GetStat(StatType.Attack) / 35d;

        var criticalChance = character.GetStat(StatType.Fuyuan) / 1000d
            * (1d + (equippedInternalSkill?.CriticalRatio ?? 0d))
            + character.GetStat(StatType.CritChance);
        criticalChance = Math.Clamp(criticalChance, 0d, 1d);

        var criticalMultiplier = 1.5d + character.GetStat(StatType.CritMult);
        return attackFactor * maxSkillPower * (1d + criticalChance * criticalMultiplier);
    }

    private static double CalculateDefence(CharacterInstance character)
    {
        var equippedInternalSkill = GetEquippedInternalSkill(character);
        var rawDefence = 150d
            + (10d + character.GetStat(StatType.Dingli) / 40d + character.GetStat(StatType.Gengu) / 70d)
            * 8d
            * (1d + (equippedInternalSkill?.DefenceRatio ?? 0d))
            + character.GetStat(StatType.Defence);
        var defenceReduction = Math.Clamp(BattleDamageCalculator.CalculateDefenceReduction(rawDefence), 0d, 0.9d);
        return character.GetStat(StatType.MaxHp) / Math.Max(0.000001d, 1d - defenceReduction) / 30d;
    }

    private static InternalSkillInstance? GetEquippedInternalSkill(CharacterInstance character) =>
        character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped);

    private static double GetMaxUsableSkillPower(CharacterInstance character, InternalSkillInstance? equippedInternalSkill)
    {
        var maxExternalPower = character.GetExternalSkills()
            .Where(static skill => skill.IsActive)
            .Select(static skill => skill.Power)
            .DefaultIfEmpty(0d)
            .Max();

        return maxExternalPower > 0d
            ? maxExternalPower
            : equippedInternalSkill?.Power ?? 0d;
    }

    private static double GetMaxWeaponStat(CharacterInstance character) =>
        new[]
        {
            character.GetStat(StatType.Quanzhang),
            character.GetStat(StatType.Jianfa),
            character.GetStat(StatType.Daofa),
            character.GetStat(StatType.Qimen),
        }.Max();

    private static int TruncateDisplayValue(double value) => (int)Math.Max(0d, value);
}

public sealed record CharacterCombatStatDisplay(
    int Attack,
    int Defence,
    double RawAttack,
    double RawDefence);
