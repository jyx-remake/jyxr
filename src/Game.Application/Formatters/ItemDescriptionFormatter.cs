using System.Text;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application.Formatters;

public static class ItemDescriptionFormatter
{
    private const string NormalLinePrefix = "◇";
    private const string ExtraLinePrefix = "◆";
    private const string ContinuationIndent = "　";

    public static string FormatBbCodeCn(ItemDefinition item, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return item switch
        {
            EquipmentDefinition equipment => FormatEquipmentBbCodeCn(equipment, [], contentRepository),
            _ => FormatItemBbCodeCn(item, contentRepository)
        };
    }

    public static string FormatBbCodeCn(EquipmentInstance equipment, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return FormatEquipmentBbCodeCn(equipment.Definition, equipment.ExtraAffixes, contentRepository);
    }

    private static string FormatItemBbCodeCn(ItemDefinition item, IContentRepository contentRepository)
    {
        var builder = new StringBuilder();
        AppendDescription(builder, item.Description);
        AppendSection(builder, "使用要求：", ItemRequirementFormatter.FormatLinesCn(item.Requirements, contentRepository), "red");
        AppendPrefixedSection(builder, "使用效果：", ItemUseEffectFormatter.FormatLinesCn(item.UseEffects, contentRepository), "yellow", NormalLinePrefix);
        AppendCooldown(builder, item.Cooldown);
        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatEquipmentBbCodeCn(
        EquipmentDefinition equipment,
        IReadOnlyList<Game.Core.Affix.AffixDefinition> extraAffixes,
        IContentRepository contentRepository)
    {
        var builder = new StringBuilder();
        AppendDescription(builder, equipment.Description);
        AppendSection(builder, "装备要求：", ItemRequirementFormatter.FormatLinesCn(equipment.Requirements, contentRepository), "red");
        AppendPrefixedSection(builder, "使用效果：", ItemUseEffectFormatter.FormatLinesCn(equipment.UseEffects, contentRepository), "yellow", NormalLinePrefix);
        AppendPrefixedSection(builder, "装备词条：", AffixFormatter.FormatEquipmentLinesCn(equipment.Affixes, contentRepository), "yellow", NormalLinePrefix);
        AppendPrefixedSection(builder, "附加词条：", AffixFormatter.FormatEquipmentLinesCn(extraAffixes, contentRepository), "green", ExtraLinePrefix);
        AppendGrantedSkills(builder, equipment, contentRepository);
        AppendCooldown(builder, equipment.Cooldown);
        return builder.ToString().TrimEnd('\n');
    }

    private static void AppendGrantedSkills(
        StringBuilder builder,
        EquipmentDefinition equipment,
        IContentRepository contentRepository)
    {
        var skillLines = new List<string>();
        foreach (var granted in equipment.GrantedSkills)
        {
            var name = contentRepository.TryGetExternalSkill(granted.SkillId, out var skill)
                ? skill.Name
                : granted.SkillId;
            skillLines.Add($"+{name}（{granted.Level}级）");
        }

        var specialLines = new List<string>();
        foreach (var granted in equipment.GrantedSpecialSkills)
        {
            if (!contentRepository.TryGetSpecialSkill(granted.SkillId, out var skill))
            {
                specialLines.Add($"+{granted.SkillId}");
                continue;
            }

            specialLines.Add($"+{skill.Name}");
            if (!string.IsNullOrWhiteSpace(skill.Description))
            {
                specialLines.Add(skill.Description.Trim());
            }
        }

        AppendPrefixedSection(builder, "携带技能：", skillLines, "#6495ED", NormalLinePrefix);
        AppendPrefixedSection(builder, "特殊技能：", specialLines, "#6495ED", NormalLinePrefix);
    }

    private static void AppendDescription(StringBuilder builder, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        AppendLine(builder, Colorize("white", description));
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> lines,
        string color)
    {
        if (lines.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        AppendLine(builder, Colorize(color, title));
        foreach (var line in lines)
        {
            AppendLine(builder, Colorize(color, line));
        }
    }

    private static void AppendPrefixedSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> lines,
        string color,
        string prefix)
    {
        if (lines.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        AppendLine(builder, Colorize(color, title));
        foreach (var line in lines)
        {
            AppendLine(builder, Colorize(color, PrefixLine(prefix, line)));
        }
    }

    private static string PrefixLine(string prefix, string line)
    {
        var normalizedLine = line.Replace("\r\n", "\n").Replace('\r', '\n');
        var parts = normalizedLine.Split('\n');
        if (parts.Length == 1)
        {
            return prefix + normalizedLine;
        }

        var builder = new StringBuilder(prefix.Length + normalizedLine.Length + parts.Length * 2);
        builder.Append(prefix);
        builder.Append(parts[0]);
        for (var index = 1; index < parts.Length; index++)
        {
            builder.Append('\n');
            builder.Append(ContinuationIndent);
            builder.Append(parts[index]);
        }

        return builder.ToString();
    }

    private static void AppendCooldown(StringBuilder builder, int cooldown)
    {
        if (cooldown <= 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        AppendLine(builder, Colorize("white", $"冷却 {cooldown} 回合"));
    }

    private static void AppendLine(StringBuilder builder, string text)
    {
        builder.Append(text);
        builder.Append('\n');
    }

    private static string Colorize(string color, string text) =>
        $"[color={color}]{text}[/color]";
}
