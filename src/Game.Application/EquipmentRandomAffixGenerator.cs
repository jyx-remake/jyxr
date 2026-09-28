using System.Globalization;
using Game.Core;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Application;

public static class EquipmentRandomAffixGenerator
{
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();
    private static readonly ExpressionFunctionRegistry ExpressionFunctions =
        new ExpressionFunctionRegistryBuilder()
            .AddLibrary(new CoreExpressionFunctions())
            .Build();

    private static readonly StatType[] RandomAttributeStats =
    [
        StatType.Quanzhang,
        StatType.Jianfa,
        StatType.Daofa,
        StatType.Qimen,
        StatType.Gengu,
        StatType.Bili,
        StatType.Fuyuan,
        StatType.Shenfa,
        StatType.Dingli,
        StatType.Wuxing,
    ];

    public static IReadOnlyList<GeneratedEquipmentAffixRoll> GenerateRolls(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round,
        int rollCount,
        IRandomService random)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(rollCount);

        var options = ResolveOptions(equipment, contentRepository, round);
        if (options.Length == 0 || rollCount == 0)
        {
            return [];
        }

        var rolls = new List<GeneratedEquipmentAffixRoll>(rollCount);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < rollCount; index++)
        {
            var roll = GenerateUniqueRoll(
                options,
                equipment.Level,
                round,
                contentRepository,
                random,
                keys);
            if (roll is null)
            {
                break;
            }

            rolls.Add(roll);
        }

        return rolls;
    }

    public static GeneratedEquipmentAffixRoll GenerateSingleRoll(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round,
        IRandomService random)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);

        var options = ResolveOptions(equipment, contentRepository, round);
        if (options.Length == 0)
        {
            throw new InvalidOperationException($"Equipment '{equipment.Id}' has no random affix options.");
        }

        return GenerateRoll(
            WeightedRandomSelector.Select(options, static option => option.Weight, random),
            equipment.Level,
            round,
            contentRepository,
            random);
    }

    public static GeneratedEquipmentAffixRoll GenerateSingleRoll(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round,
        IRandomService random,
        IReadOnlyList<IReadOnlyList<AffixDefinition>> excludedGroups)
        => TryGenerateSingleRoll(equipment, contentRepository, round, random, excludedGroups)
            ?? throw new InvalidOperationException(
                $"Equipment '{equipment.Id}' cannot generate a random affix outside its current affixes.");

    public static GeneratedEquipmentAffixRoll? TryGenerateSingleRoll(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round,
        IRandomService random,
        IReadOnlyList<IReadOnlyList<AffixDefinition>> excludedGroups)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentNullException.ThrowIfNull(excludedGroups);

        var options = ResolveOptions(equipment, contentRepository, round)
            .Select(option => option.Kind == EquipmentRandomAffixKind.Talent
                ? option with { Pool = option.Pool.Where(id => excludedGroups.All(group =>
                    !Matches(group, option.Kind, contentRepository, talentId: id))).ToArray() }
                : option)
            .Where(option => option.Kind == EquipmentRandomAffixKind.Talent
                ? option.Pool.Count > 0
                : excludedGroups.All(group => !Matches(group, option.Kind, contentRepository, weaponType: option.WeaponType)))
            .ToArray();

        return options.Length == 0 ? null : GenerateRoll(
            WeightedRandomSelector.Select(options, static option => option.Weight, random),
            equipment.Level, round, contentRepository, random);
    }

    private static EquipmentRandomAffixOptionDefinition[] ResolveOptions(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round) =>
        contentRepository.GetEquipmentRandomAffixTables()
            .Where(table => EvaluateBoolean(table.When, equipment.Level, round, skillHard: null, $"table '{table.Id}' when"))
            .SelectMany(static table => table.Options)
            .ToArray();

    private static GeneratedEquipmentAffixRoll? GenerateUniqueRoll(
        IReadOnlyList<EquipmentRandomAffixOptionDefinition> options,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random,
        ISet<string> existingKeys)
    {
        for (var attempt = 0; attempt < 1024; attempt++)
        {
            var option = WeightedRandomSelector.Select(options, static value => value.Weight, random);
            var roll = GenerateRoll(option, itemLevel, round, contentRepository, random);
            if (existingKeys.Add(roll.Key))
            {
                return roll;
            }
        }

        return null;
    }

    private static GeneratedEquipmentAffixRoll GenerateRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random) =>
        option.Kind switch
        {
            EquipmentRandomAffixKind.AttackCombo => BuildAttackComboRoll(option, itemLevel, round, random),
            EquipmentRandomAffixKind.DefenceCombo => BuildDefenceComboRoll(option, itemLevel, round, random),
            EquipmentRandomAffixKind.RandomAttribute => BuildRandomAttributeRoll(option, itemLevel, round, random),
            EquipmentRandomAffixKind.Talent => BuildTalentRoll(option, contentRepository, random),
            EquipmentRandomAffixKind.Accuracy => BuildStatRangeRoll(StatType.Accuracy, option, itemLevel, round, random),
            EquipmentRandomAffixKind.ExternalSkillBonus => BuildExternalSkillBonusRoll(option, itemLevel, round, contentRepository, random),
            EquipmentRandomAffixKind.InternalSkillBonus => BuildInternalSkillBonusRoll(option, itemLevel, round, contentRepository, random),
            EquipmentRandomAffixKind.FormSkillBonus => BuildFormSkillBonusRoll(option, itemLevel, round, contentRepository, random),
            EquipmentRandomAffixKind.LegendSkillBonus => BuildLegendSkillBonusRoll(option, itemLevel, round, contentRepository, random),
            EquipmentRandomAffixKind.CritChance => BuildDirectCritChanceRoll(option, itemLevel, round, random),
            EquipmentRandomAffixKind.CritMult => BuildStatRangeRoll(StatType.CritMult, option, itemLevel, round, random),
            EquipmentRandomAffixKind.Lifesteal => BuildStatRangeRoll(StatType.Lifesteal, option, itemLevel, round, random),
            EquipmentRandomAffixKind.Speed => BuildSpeedRoll(option, random),
            EquipmentRandomAffixKind.AntiDebuff => BuildStatRangeRoll(StatType.AntiDebuff, option, itemLevel, round, random),
            EquipmentRandomAffixKind.WeaponBonus => BuildWeaponBonusRoll(option, itemLevel, round, random),
            _ => throw new InvalidOperationException($"Unsupported equipment random affix kind '{option.Kind}'."),
        };

    private static GeneratedEquipmentAffixRoll BuildAttackComboRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        var attack = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        var critChance = RollRange(option, 1, itemLevel, round, skillHard: null, random);
        return CreateRoll(
            EquipmentRandomAffixKind.AttackCombo,
            [
                new StatModifierAffix(StatType.Attack, ModifierValue.Add(attack)),
                new StatModifierAffix(StatType.CritChance, ModifierValue.Add(AsRatio(critChance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildDefenceComboRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        var defence = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        var antiCritChance = RollRange(option, 1, itemLevel, round, skillHard: null, random);
        return CreateRoll(
            EquipmentRandomAffixKind.DefenceCombo,
            [
                new StatModifierAffix(StatType.Defence, ModifierValue.Add(defence)),
                new StatModifierAffix(StatType.AntiCritChance, ModifierValue.Add(AsRatio(antiCritChance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildRandomAttributeRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        var stat = PickRandom(RandomAttributeStats, random);
        var value = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        return CreateRoll(
            EquipmentRandomAffixKind.RandomAttribute,
            [new StatModifierAffix(stat, ModifierValue.Add(value))]);
    }

    private static GeneratedEquipmentAffixRoll BuildTalentRoll(
        EquipmentRandomAffixOptionDefinition option,
        IContentRepository contentRepository,
        IRandomService random)
    {
        var talentId = PickRandom(option.Pool, random);
        var affix = new GrantTalentAffix(talentId);
        affix.Resolve(contentRepository);
        return CreateRoll(EquipmentRandomAffixKind.Talent, [affix]);
    }

    private static GeneratedEquipmentAffixRoll BuildExternalSkillBonusRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random)
    {
        var skill = PickRandom(contentRepository.GetExternalSkills()
            .Where(skill => IsCandidate(option, itemLevel, round, skill.Hard))
            .ToArray(), random);
        var value = RollRange(option, 0, itemLevel, round, skill.Hard, random);
        return CreateRoll(
            EquipmentRandomAffixKind.ExternalSkillBonus,
            [new SkillBonusModifierAffix(skill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildInternalSkillBonusRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random)
    {
        var skill = PickRandom(contentRepository.GetInternalSkills()
            .Where(skill => IsCandidate(option, itemLevel, round, skill.Hard))
            .ToArray(), random);
        var value = RollRange(option, 0, itemLevel, round, skill.Hard, random);
        return CreateRoll(
            EquipmentRandomAffixKind.InternalSkillBonus,
            [new SkillBonusModifierAffix(skill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildFormSkillBonusRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random)
    {
        var formSkill = PickRandom(contentRepository.GetExternalSkills()
            .SelectMany(static skill => skill.FormSkills)
            .Concat(contentRepository.GetInternalSkills().SelectMany(static skill => skill.FormSkills))
            .Where(skill => IsCandidate(option, itemLevel, round, skill.Hard))
            .ToArray(), random);
        var value = RollRange(option, 0, itemLevel, round, formSkill.Hard, random);
        return CreateRoll(
            EquipmentRandomAffixKind.FormSkillBonus,
            [new SkillBonusModifierAffix(formSkill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildLegendSkillBonusRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        IRandomService random)
    {
        var candidate = PickRandom(contentRepository.GetLegendSkills()
            .Select(skill => (Skill: skill, Hard: ResolveLegendSkillHard(skill, contentRepository)))
            .Where(entry => entry.Hard is not null && IsCandidate(option, itemLevel, round, entry.Hard.Value))
            .ToArray(), random);
        var power = RollRange(option, 0, itemLevel, round, candidate.Hard!.Value, random);
        var chance = RollRange(option, 1, itemLevel, round, candidate.Hard.Value, random);
        return CreateRoll(
            EquipmentRandomAffixKind.LegendSkillBonus,
            [
                new SkillBonusModifierAffix(candidate.Skill.Id, ModifierValue.Add(AsRatio(power))),
                new LegendSkillChanceModifierAffix(candidate.Skill.Id, ModifierValue.Add(AsRatio(chance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildDirectCritChanceRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        var percent = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        return CreateRoll(
            EquipmentRandomAffixKind.CritChance,
            [new StatModifierAffix(StatType.CritChance, ModifierValue.Add(AsRatio(percent)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildSpeedRoll(
        EquipmentRandomAffixOptionDefinition option,
        IRandomService random)
    {
        var value = double.Parse(PickRandom(option.Pool, random), CultureInfo.InvariantCulture);
        return CreateRoll(
            EquipmentRandomAffixKind.Speed,
            [new StatModifierAffix(StatType.Speed, ModifierValue.Add(value))]);
    }

    private static GeneratedEquipmentAffixRoll BuildWeaponBonusRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        if (option.WeaponType is null)
        {
            throw new InvalidOperationException("Weapon bonus affix option requires weaponType.");
        }

        var value = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        return CreateRoll(
            EquipmentRandomAffixKind.WeaponBonus,
            [new WeaponBonusModifierAffix(option.WeaponType.Value, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildStatRangeRoll(
        StatType stat,
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IRandomService random)
    {
        var value = RollRange(option, 0, itemLevel, round, skillHard: null, random);
        var delta = stat is StatType.Accuracy or StatType.CritMult or StatType.Lifesteal or StatType.AntiDebuff
            ? AsRatio(value)
            : value;
        return CreateRoll(option.Kind, [new StatModifierAffix(stat, ModifierValue.Add(delta))]);
    }

    private static double RollRange(
        EquipmentRandomAffixOptionDefinition option,
        int index,
        int itemLevel,
        int round,
        double? skillHard,
        IRandomService random)
    {
        if (option.Ranges.Count <= index)
        {
            throw new InvalidOperationException(
                $"Equipment random affix option '{option.Kind}' is missing range index {index}.");
        }

        var range = option.Ranges[index];
        var min = EvaluateNumber(range.Min, itemLevel, round, skillHard, $"option '{option.Kind}' range {index} min");
        var max = EvaluateNumber(range.Max, itemLevel, round, skillHard, $"option '{option.Kind}' range {index} max");
        if (min > max)
        {
            throw new InvalidOperationException(
                $"Equipment random affix option '{option.Kind}' range {index} has min {min} greater than max {max}.");
        }

        if (range.Mode == EquipmentRandomAffixRangeMode.Integer)
        {
            var intMin = ExpressionValue.FromNumber(min).AsInt32($"Option '{option.Kind}' range {index} min");
            var intMax = ExpressionValue.FromNumber(max).AsInt32($"Option '{option.Kind}' range {index} max");
            return random.Next(intMin, checked(intMax + 1));
        }

        var value = min == max ? min : min + random.NextDouble() * (max - min);
        return Math.Round(value, range.DecimalPlaces);
    }

    private static bool IsCandidate(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        double skillHard) => option.CandidateWhen is not null &&
        EvaluateBoolean(option.CandidateWhen, itemLevel, round, skillHard, $"option '{option.Kind}' candidateWhen");

    private static bool EvaluateBoolean(
        ParsedExpression expression,
        int itemLevel,
        int round,
        double? skillHard,
        string context) =>
        ExpressionEvaluator.Evaluate(expression, CreateEnvironment(itemLevel, round, skillHard)).AsBoolean(context);

    private static double EvaluateNumber(
        ParsedExpression expression,
        int itemLevel,
        int round,
        double? skillHard,
        string context) =>
        ExpressionEvaluator.Evaluate(expression, CreateEnvironment(itemLevel, round, skillHard)).AsNumber(context);

    private static ExpressionEnvironment CreateEnvironment(int itemLevel, int round, double? skillHard)
    {
        var variables = new Dictionary<string, ExpressionValue>(StringComparer.Ordinal)
        {
            ["item_level"] = ExpressionValue.FromNumber(itemLevel),
            ["round"] = ExpressionValue.FromNumber(round),
        };
        if (skillHard is { } value)
        {
            variables["skill_hard"] = ExpressionValue.FromNumber(value);
        }

        return new ExpressionEnvironment(new DictionaryExpressionVariableResolver(variables), ExpressionFunctions);
    }

    private static GeneratedEquipmentAffixRoll CreateRoll(
        EquipmentRandomAffixKind kind,
        IReadOnlyList<AffixDefinition> affixes) =>
        new(GetKey(kind, affixes), kind, affixes);

    private static string GetKey(
        EquipmentRandomAffixKind kind,
        IReadOnlyList<AffixDefinition> affixes) => kind switch
        {
            EquipmentRandomAffixKind.AttackCombo => "attack_combo",
            EquipmentRandomAffixKind.DefenceCombo => "defence_combo",
            EquipmentRandomAffixKind.RandomAttribute => "random_attribute",
            EquipmentRandomAffixKind.Talent => $"talent:{((GrantTalentAffix)affixes[0]).TalentId}",
            EquipmentRandomAffixKind.Accuracy => "accuracy",
            EquipmentRandomAffixKind.ExternalSkillBonus => "external_skill_bonus",
            EquipmentRandomAffixKind.InternalSkillBonus => "internal_skill_bonus",
            EquipmentRandomAffixKind.FormSkillBonus => "form_skill_bonus",
            EquipmentRandomAffixKind.LegendSkillBonus => "legend_skill_bonus",
            EquipmentRandomAffixKind.CritChance => "crit_chance",
            EquipmentRandomAffixKind.CritMult => "crit_mult",
            EquipmentRandomAffixKind.Lifesteal => "lifesteal",
            EquipmentRandomAffixKind.Speed => "speed",
            EquipmentRandomAffixKind.AntiDebuff => "anti_debuff",
            EquipmentRandomAffixKind.WeaponBonus =>
                $"weapon_bonus:{((WeaponBonusModifierAffix)affixes[0]).WeaponType}",
            _ => throw new InvalidOperationException($"Unsupported equipment random affix kind '{kind}'."),
        };

    private static bool Matches(
        IReadOnlyList<AffixDefinition> group,
        EquipmentRandomAffixKind kind,
        IContentRepository contentRepository,
        string? talentId = null,
        WeaponType? weaponType = null) => kind switch
        {
            EquipmentRandomAffixKind.AttackCombo => group is
                [StatModifierAffix { Stat: StatType.Attack }, StatModifierAffix { Stat: StatType.CritChance }],
            EquipmentRandomAffixKind.DefenceCombo => group is
                [StatModifierAffix { Stat: StatType.Defence }, StatModifierAffix { Stat: StatType.AntiCritChance }],
            EquipmentRandomAffixKind.RandomAttribute => group is [StatModifierAffix stat]
                && RandomAttributeStats.Contains(stat.Stat),
            EquipmentRandomAffixKind.Talent => group is [GrantTalentAffix talent]
                && talent.TalentId == talentId,
            EquipmentRandomAffixKind.ExternalSkillBonus => group is [SkillBonusModifierAffix skill]
                && contentRepository.TryGetExternalSkill(skill.SkillId, out _),
            EquipmentRandomAffixKind.InternalSkillBonus => group is [SkillBonusModifierAffix skill]
                && contentRepository.TryGetInternalSkill(skill.SkillId, out _),
            EquipmentRandomAffixKind.FormSkillBonus => group is [SkillBonusModifierAffix skill]
                && !contentRepository.TryGetExternalSkill(skill.SkillId, out _)
                && !contentRepository.TryGetInternalSkill(skill.SkillId, out _),
            EquipmentRandomAffixKind.LegendSkillBonus => group is
                [SkillBonusModifierAffix, LegendSkillChanceModifierAffix],
            EquipmentRandomAffixKind.WeaponBonus => group is [WeaponBonusModifierAffix weapon]
                && weapon.WeaponType == weaponType,
            _ => group is [StatModifierAffix stat]
                && stat.Stat == (kind switch
                {
                    EquipmentRandomAffixKind.Accuracy => StatType.Accuracy,
                    EquipmentRandomAffixKind.CritChance => StatType.CritChance,
                    EquipmentRandomAffixKind.CritMult => StatType.CritMult,
                    EquipmentRandomAffixKind.Lifesteal => StatType.Lifesteal,
                    EquipmentRandomAffixKind.Speed => StatType.Speed,
                    EquipmentRandomAffixKind.AntiDebuff => StatType.AntiDebuff,
                    _ => throw new InvalidOperationException($"Unsupported equipment random affix kind '{kind}'."),
                }),
        };

    private static double? ResolveLegendSkillHard(
        LegendSkillDefinition skill,
        IContentRepository contentRepository)
    {
        if (contentRepository.TryGetExternalSkill(skill.StartSkill, out var startSkill))
        {
            return startSkill.Hard;
        }

        return contentRepository.GetExternalSkills()
            .SelectMany(static externalSkill => externalSkill.FormSkills)
            .Concat(contentRepository.GetInternalSkills().SelectMany(static internalSkill => internalSkill.FormSkills))
            .FirstOrDefault(formSkill => string.Equals(formSkill.Id, skill.StartSkill, StringComparison.Ordinal))
            ?.Hard;
    }

    private static T PickRandom<T>(IReadOnlyList<T> values, IRandomService random)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Random selection candidates cannot be empty.");
        }

        return values[random.Next(0, values.Count)];
    }

    private static double AsRatio(double value) => value / 100d;
}
