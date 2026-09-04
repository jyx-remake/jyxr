using System.Globalization;
using System.Text;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application.Formatters;

/// <summary>
/// Renders a character title exactly like the legacy title tooltip:
/// description, red +attack %, green +defence %, cyan 奥义 lines, then the
/// green (√) passive affix lines (被动增益).
/// </summary>
public static class TitleDescriptionFormatter
{
    public static string FormatBbCodeCn(
        CharacterTitleDefinition title,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(contentRepository);

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title.Description))
        {
            AppendLine(builder, title.Description.Trim());
        }

        // The table attack/defence fields are already percentages (legacy
        // TitleInstance normalized them with Level * value * 0.01 before the
        // template multiplied back by 100), so they render as-is. Only the
        // fractional aoyi probability is scaled.
        AppendRedLine(builder, $"+攻击 {FormatNumber(title.Attack)}%");
        AppendGreenLine(builder, $"+防御 {FormatNumber(title.Defence)}%");
        AppendCyanLine(builder, $"+奥义威力 {FormatNumber(title.AoyiPowerAdd)}");
        AppendCyanLine(builder, $"+奥义发动概率 {FormatNumber(title.AoyiProbabilityAdd * 100d)}%");
        if (title.Affixes.Count > 0)
        {
            AppendLine(builder, "被动增益：");
            foreach (var affix in title.Affixes)
            {
                // Attack/defence stat affixes mirror the header lines above;
                // the legacy trigger list never repeated them.
                if (affix is StatModifierAffix statModifier &&
                    (statModifier.Stat == StatType.Attack || statModifier.Stat == StatType.Defence))
                {
                    continue;
                }

                AppendGreenLine(builder, $"(√){AffixFormatter.FormatCn(affix, contentRepository)}");
            }
        }

        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatNumber(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero).ToString("0.######", CultureInfo.InvariantCulture);

    private static void AppendRedLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("red", text));

    private static void AppendGreenLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("green", text));

    private static void AppendCyanLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("cyan", text));

    private static void AppendLine(StringBuilder builder, string text)
    {
        builder.Append(text);
        builder.Append('\n');
    }

    private static string Colorize(string color, string text) =>
        $"[color={color}]{text}[/color]";
}
