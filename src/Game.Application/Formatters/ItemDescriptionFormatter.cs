using System.Text;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application.Formatters;

public static class ItemDescriptionFormatter
{
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
        AppendSection(builder, "使用效果：", ItemUseEffectFormatter.FormatLinesCn(item.UseEffects, contentRepository), "yellow");
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
        AppendSection(builder, "使用效果：", ItemUseEffectFormatter.FormatLinesCn(equipment.UseEffects, contentRepository), "yellow");
        AppendSection(builder, "装备词条：", AffixFormatter.FormatLinesCn(equipment.Affixes, contentRepository), "yellow");
        AppendSection(builder, "附加词条：", AffixFormatter.FormatLinesCn(extraAffixes, contentRepository), "green");
        AppendCooldown(builder, equipment.Cooldown);
        return builder.ToString().TrimEnd('\n');
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

        AppendLine(builder, Colorize("black", $"冷却 {cooldown} 回合"));
    }

    private static void AppendLine(StringBuilder builder, string text)
    {
        builder.Append(text);
        builder.Append('\n');
    }

    private static string Colorize(string color, string text) =>
        $"[color={color}]{text}[/color]";
}
