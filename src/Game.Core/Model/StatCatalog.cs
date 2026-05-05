using System.Reflection;
using System.Text.Json.Serialization;

namespace Game.Core.Model;

public static class StatCatalog
{
    private sealed record StatDefinition(
        StatType StatType,
        string DisplayNameCn,
        bool IsTenDimension = false);

    private static readonly IReadOnlyList<StatDefinition> Definitions =
    [
        new(StatType.Quanzhang, "拳掌", IsTenDimension: true),
        new(StatType.Jianfa, "剑法", IsTenDimension: true),
        new(StatType.Daofa, "刀法", IsTenDimension: true),
        new(StatType.Qimen, "奇门", IsTenDimension: true),
        new(StatType.Bili, "臂力", IsTenDimension: true),
        new(StatType.Shenfa, "身法", IsTenDimension: true),
        new(StatType.Wuxing, "悟性", IsTenDimension: true),
        new(StatType.Fuyuan, "福缘", IsTenDimension: true),
        new(StatType.Gengu, "根骨", IsTenDimension: true),
        new(StatType.Dingli, "定力", IsTenDimension: true),
        new(StatType.Wuxue, "武学点"),
        new(StatType.MaxHp, "气血上限"),
        new(StatType.MaxMp, "内力上限"),
        new(StatType.Attack, "攻击力"),
        new(StatType.Defence, "防御力"),
        new(StatType.Accuracy, "命中率"),
        new(StatType.CritChance, "暴击率"),
        new(StatType.CritMult, "暴击伤害"),
        new(StatType.AntiCritChance, "抗暴率"),
        new(StatType.Lifesteal, "吸血"),
        new(StatType.AntiDebuff, "抗异常"),
        new(StatType.Speed, "集气速度"),
        new(StatType.Movement, "移动力"),
    ];

    private static readonly IReadOnlyDictionary<StatType, StatDefinition> DefinitionsByType =
        Definitions.ToDictionary(definition => definition.StatType);

    private static readonly IReadOnlyDictionary<string, StatType> Aliases =
        BuildAliases();

    public static IReadOnlyList<StatType> TenDimensionStats { get; } =
        Definitions.Where(static definition => definition.IsTenDimension)
            .Select(static definition => definition.StatType)
            .ToArray();

    public static IReadOnlyList<StatType> MinusMaxPointsStats { get; } =
    [
        StatType.MaxHp,
        StatType.MaxMp,
        ..TenDimensionStats,
    ];

    public static string GetCode(StatType statType)
    {
        var member = typeof(StatType).GetMember(statType.ToString(), BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault()
            ?? throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
        var attribute = member.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()
            ?? throw new InvalidOperationException($"Stat '{statType}' is missing json enum name metadata.");
        return attribute.Name;
    }

    public static string GetDisplayNameCn(StatType statType)
    {
        if (DefinitionsByType.TryGetValue(statType, out var definition))
        {
            return definition.DisplayNameCn;
        }

        throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
    }

    public static StatType Parse(string raw)
    {
        if (TryParse(raw, out var statType))
        {
            return statType;
        }

        throw new InvalidOperationException($"Unsupported stat name '{raw}'.");
    }

    public static bool TryParse(string? raw, out StatType statType)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Aliases.TryGetValue(raw.Trim(), out statType))
        {
            return true;
        }

        statType = default;
        return false;
    }

    private static IReadOnlyDictionary<string, StatType> BuildAliases()
    {
        var aliases = new Dictionary<string, StatType>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in Definitions)
        {
            aliases.Add(definition.DisplayNameCn, definition.StatType);
            aliases.Add(GetCode(definition.StatType), definition.StatType);
        }
        aliases.Add("maxhp", StatType.MaxHp);
        aliases.Add("maxmp", StatType.MaxMp);

        return aliases;
    }
}
