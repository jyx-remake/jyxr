using System.Globalization;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Application.Formatters;

public static class AffixFormatter
{
    private enum ValueDisplayKind
    {
        Plain,
        Percentage
    }

    private readonly record struct ValueDisplaySpec(ValueDisplayKind Kind, double AdditiveScale = 1d);

    public static string FormatCn(AffixDefinition affix, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(affix);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return affix switch
        {
            StatModifierAffix statModifier => $"{FormatterTextCn.GetStatNameCn(statModifier.Stat)}{FormatStatModifierValueCn(statModifier.Stat, statModifier.Value)}",
            BuffLevelStatModifierAffix buffLevelStatModifier => FormatBuffLevelStatModifierCn(buffLevelStatModifier),
            GrantTalentAffix grantTalent => $"天赋「{FormatterTextCn.ResolveTalentName(grantTalent.TalentId, contentRepository)}」",
            GrantModelAffix grantModel => $"时装「{GetModelDisplayText(grantModel)}」",
            SkillBonusModifierAffix skillBonus => $"技能「{FormatterTextCn.ResolveSkillName(skillBonus.SkillId, contentRepository)}」威力{FormatModifierValueCn(skillBonus.Value, new ValueDisplaySpec(ValueDisplayKind.Percentage, 100))}",
            WeaponBonusModifierAffix weaponBonus => $"{FormatterTextCn.GetWeaponTypeNameCn(weaponBonus.WeaponType)}类武功威力{FormatModifierValueCn(weaponBonus.Value, new ValueDisplaySpec(ValueDisplayKind.Percentage, 100))}",
            LegendSkillChanceModifierAffix legendChance => $"奥义「{FormatterTextCn.ResolveSkillName(legendChance.SkillId, contentRepository)}」触发率{FormatModifierValueCn(legendChance.Value, new ValueDisplaySpec(ValueDisplayKind.Percentage, 100))}",
            _ => throw new NotSupportedException($"Unsupported affix type '{affix.GetType().Name}'.")
        };
    }

    public static string FormatCn(SkillAffixDefinition affix, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);

        var effectText = FormatCn(affix.Effect, contentRepository);
        var parts = new List<string>(2);

        if (affix.MinimumLevel > 1)
        {
            parts.Add($"{affix.MinimumLevel}级解锁");
        }

        if (affix.RequiresEquippedInternalSkill)
        {
            parts.Add("装备生效");
        }

        return parts.Count == 0
            ? effectText
            : $"{string.Join('，', parts)}：{effectText}";
    }

    public static IReadOnlyList<string> FormatLinesCn(
        IEnumerable<AffixDefinition> affixes,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(affixes);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return affixes.Select(affix => FormatCn(affix, contentRepository)).ToList();
    }

    public static IReadOnlyList<string> FormatEquipmentLinesCn(
        IEnumerable<AffixDefinition> affixes,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(affixes);
        ArgumentNullException.ThrowIfNull(contentRepository);

        var list = affixes.ToList();
        var lines = new List<string>(list.Count);

        for (var index = 0; index < list.Count; index++)
        {
            var current = list[index];
            if (TryFormatMergedEquipmentLine(list, index, contentRepository, out var mergedLine, out var consumedCount))
            {
                lines.Add(mergedLine);
                index += consumedCount - 1;
                continue;
            }

            lines.Add(FormatCn(current, contentRepository));
        }

        return lines;
    }

    public static IReadOnlyList<string> FormatLinesCn(
        IEnumerable<SkillAffixDefinition> affixes,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(affixes);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return affixes.Select(affix => FormatCn(affix, contentRepository)).ToList();
    }

    private static string GetModelDisplayText(GrantModelAffix affix) =>
        string.IsNullOrWhiteSpace(affix.Description) ? affix.ModelId : affix.Description;

    private static bool TryFormatMergedEquipmentLine(
        IReadOnlyList<AffixDefinition> affixes,
        int index,
        IContentRepository contentRepository,
        out string line,
        out int consumedCount)
    {
        line = string.Empty;
        consumedCount = 0;

        if (index + 1 >= affixes.Count)
        {
            return false;
        }

        if (affixes[index] is StatModifierAffix attack
            && attack.Stat == StatType.Attack
            && affixes[index + 1] is StatModifierAffix critChance
            && critChance.Stat == StatType.CritChance)
        {
            line = $"攻击力{FormatStatModifierValueCn(StatType.Attack, attack.Value)}，暴击率{FormatStatModifierValueCn(StatType.CritChance, critChance.Value)}";
            consumedCount = 2;
            return true;
        }

        if (affixes[index] is StatModifierAffix defence
            && defence.Stat == StatType.Defence
            && affixes[index + 1] is StatModifierAffix antiCritChance
            && antiCritChance.Stat == StatType.AntiCritChance)
        {
            line = $"防御力{FormatStatModifierValueCn(StatType.Defence, defence.Value)}，抗暴击率{FormatStatModifierValueCn(StatType.AntiCritChance, antiCritChance.Value)}";
            consumedCount = 2;
            return true;
        }

        if (affixes[index] is SkillBonusModifierAffix legendSkillBonus
            && affixes[index + 1] is LegendSkillChanceModifierAffix legendSkillChance
            && string.Equals(legendSkillBonus.SkillId, legendSkillChance.SkillId, StringComparison.Ordinal))
        {
            var skillName = FormatterTextCn.ResolveSkillName(legendSkillBonus.SkillId, contentRepository);
            line = $"奥义「{skillName}」威力{FormatModifierValueCn(legendSkillBonus.Value, new ValueDisplaySpec(ValueDisplayKind.Percentage, 100))}，触发率{FormatModifierValueCn(legendSkillChance.Value, new ValueDisplaySpec(ValueDisplayKind.Percentage, 100))}";
            consumedCount = 2;
            return true;
        }

        return false;
    }

    private static string FormatStatModifierValueCn(StatType statType, ModifierValue value) =>
        FormatModifierValueCn(value, GetStatValueDisplaySpec(statType));

    private static string FormatModifierValueCn(ModifierValue value, ValueDisplaySpec displaySpec) =>
        value.Op switch
        {
            ModifierOp.Add => FormatAdditiveValueCn(value.Delta, displaySpec),
            ModifierOp.Increase => FormatMultiplierValueCn(value.Delta),
            ModifierOp.More => FormatMoreValueCn(value.Delta),
            ModifierOp.PostAdd => FormatAdditiveValueCn(value.Delta, displaySpec),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    private static string FormatAdditiveValueCn(double value, ValueDisplaySpec displaySpec)
    {
        var scaledValue = Math.Round(
            value * displaySpec.AdditiveScale,
            6,
            MidpointRounding.AwayFromZero);
        return displaySpec.Kind switch
        {
            ValueDisplayKind.Plain => $" {FormatSignedNumber(scaledValue)}",
            ValueDisplayKind.Percentage => $" {FormatSignedNumber(scaledValue)}%",
            _ => throw new ArgumentOutOfRangeException(nameof(displaySpec), displaySpec, null)
        };
    }

    private static string FormatMultiplierValueCn(double value)
    {
        var percentText = FormatUnsignedNumber(Math.Round(Math.Abs(value) * 100d, 6, MidpointRounding.AwayFromZero));
        return value >= 0
            ? $"提高{percentText}%"
            : $"降低{percentText}%";
    }

    private static string FormatMoreValueCn(double value)
    {
        var percentText = FormatUnsignedNumber(Math.Round(Math.Abs(value - 1d) * 100d, 6, MidpointRounding.AwayFromZero));
        return value >= 1d
            ? $"提高{percentText}%"
            : $"降低{percentText}%";
    }

    private static string FormatSignedNumber(double value)
    {
        var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
        var numberText = rounded.ToString("0.######", CultureInfo.InvariantCulture);
        return rounded >= 0 ? $"+{numberText}" : numberText;
    }

    private static string FormatUnsignedNumber(double value)
    {
        var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
        return rounded.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string FormatBuffLevelStatModifierCn(BuffLevelStatModifierAffix affix)
    {
        var parts = new List<string>(3);
        var displaySpec = GetStatValueDisplaySpec(affix.Stat);

        if (Math.Abs(affix.AddBase) > double.Epsilon)
        {
            parts.Add(FormatAdditiveValueCn(affix.AddBase, displaySpec).TrimStart());
        }

        if (Math.Abs(affix.AddPerLevel) > double.Epsilon)
        {
            parts.Add($"每级{FormatAdditiveValueCn(affix.AddPerLevel, displaySpec)}");
        }

        if (Math.Abs(affix.MulPerLevel) > double.Epsilon)
        {
            parts.Add($"每级{FormatMultiplierValueCn(affix.MulPerLevel)}");
        }

        if (parts.Count == 0)
        {
            parts.Add("+0");
        }

        return $"{FormatterTextCn.GetStatNameCn(affix.Stat)} {string.Join('，', parts)}";
    }

    private static ValueDisplaySpec GetStatValueDisplaySpec(StatType statType) =>
        statType switch
        {
            StatType.Evasion => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.Accuracy => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.CritChance => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.CritMult => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.AntiCritChance => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.Lifesteal => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            StatType.AntiDebuff => new ValueDisplaySpec(ValueDisplayKind.Percentage, 100d),
            _ => new ValueDisplaySpec(ValueDisplayKind.Plain)
        };

}
