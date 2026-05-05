using System.Globalization;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application.Formatters;

internal static class FormatterTextCn
{
    public static string ResolveTalentName(string talentId, IContentRepository contentRepository)
        => contentRepository.TryGetTalent(talentId, out var talent)
            ? talent.Name
            : talentId;

    public static string ResolveExternalSkillName(string skillId, IContentRepository contentRepository)
        => contentRepository.TryGetExternalSkill(skillId, out var skill)
            ? skill.Name
            : skillId;

    public static string ResolveInternalSkillName(string skillId, IContentRepository contentRepository)
        => contentRepository.TryGetInternalSkill(skillId, out var skill)
            ? skill.Name
            : skillId;

    public static string ResolveSpecialSkillName(string skillId, IContentRepository contentRepository)
        => contentRepository.TryGetSpecialSkill(skillId, out var skill)
            ? skill.Name
            : skillId;

    public static string ResolveSkillName(string skillId, IContentRepository contentRepository)
    {
        if (contentRepository.TryGetExternalSkill(skillId, out var externalSkill))
        {
            return externalSkill.Name;
        }

        if (contentRepository.TryGetInternalSkill(skillId, out var internalSkill))
        {
            return internalSkill.Name;
        }

        if (contentRepository.TryGetSpecialSkill(skillId, out var specialSkill))
        {
            return specialSkill.Name;
        }

        var legendSkill = contentRepository.GetLegendSkills()
            .FirstOrDefault(definition =>
                string.Equals(definition.Id, skillId, StringComparison.Ordinal) ||
                string.Equals(definition.Name, skillId, StringComparison.Ordinal));

        return legendSkill?.Name ?? skillId;
    }

    public static string ResolveBuffName(string buffId, IContentRepository contentRepository)
        => contentRepository.TryGetBuff(buffId, out var buff)
            ? buff.Name
            : buffId;

    public static string GetStatNameCn(StatType statType) => StatCatalog.GetDisplayNameCn(statType);

    public static string GetWeaponTypeNameCn(WeaponType weaponType) =>
        weaponType switch
        {
            WeaponType.Quanzhang => "拳掌",
            WeaponType.Jianfa => "剑法",
            WeaponType.Daofa => "刀法",
            WeaponType.Qimen => "奇门",
            WeaponType.InternalSkill => "内功",
            WeaponType.Unknown => "未知",
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
        };

    public static string FormatNumber(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero).ToString("0.######", CultureInfo.InvariantCulture);

    public static string FormatPercent(double ratio) => $"{FormatNumber(ratio * 100d)}%";
}
