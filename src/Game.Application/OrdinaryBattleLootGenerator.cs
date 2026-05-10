using System.Globalization;
using Game.Core;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Application;

public static class OrdinaryBattleLootGenerator
{
    private const double ItemDropChance = 0.1d;
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

    public static IReadOnlyList<OrdinaryBattleRewardDrop> Generate(
        BattleState state,
        IContentRepository contentRepository,
        int round,
        int playerTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);

        var drops = new List<OrdinaryBattleRewardDrop>();
        foreach (var enemyUnit in state.Units.Where(unit => unit.Team != playerTeam))
        {
            if (!Probability.RollChance(ItemDropChance))
            {
                continue;
            }

            var candidates = ResolveDropCandidates(contentRepository, enemyUnit.Character.Level);
            if (candidates.Count == 0)
            {
                continue;
            }

            var item = PickRandom(candidates);
            if (item is EquipmentDefinition equipment)
            {
                drops.Add(new OrdinaryBattleEquipmentRewardDrop(
                    equipment,
                    GenerateEquipmentRolls(equipment, contentRepository, round)));
                continue;
            }

            drops.Add(new OrdinaryBattleStackRewardDrop(item, 1));
        }

        return drops;
    }

    public static IReadOnlyList<GeneratedEquipmentAffixRoll> GenerateEquipmentRolls(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);

        var tables = contentRepository.GetEquipmentRandomAffixTables()
            .Where(table => equipment.Level >= table.MinItemLevel && equipment.Level <= table.MaxItemLevel)
            .ToArray();
        if (tables.Length == 0)
        {
            return [];
        }

        var options = tables
            .SelectMany(static table => table.Options)
            .ToArray();
        if (options.Length == 0)
        {
            return [];
        }

        var rollCount = RollEquipmentAffixCount();
        var rolls = new List<GeneratedEquipmentAffixRoll>(rollCount);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var totalWeight = options.Sum(option => option.Weight);

        for (var index = 0; index < rollCount; index++)
        {
            var roll = GenerateUniqueRoll(
                options,
                totalWeight,
                equipment.Level,
                round,
                contentRepository,
                keys);
            if (roll is null)
            {
                break;
            }

            rolls.Add(roll);
        }

        return rolls;
    }

    private static IReadOnlyList<ItemDefinition> ResolveDropCandidates(
        IContentRepository contentRepository,
        int enemyLevel) =>
        contentRepository.GetItems()
            .Where(item => item.CanDrop && enemyLevel >= (item.Level - 1) * 5)
            .ToArray();

    private static GeneratedEquipmentAffixRoll? GenerateUniqueRoll(
        IReadOnlyList<EquipmentRandomAffixOptionDefinition> options,
        int totalWeight,
        int itemLevel,
        int round,
        IContentRepository contentRepository,
        ISet<string> existingKeys)
    {
        for (var attempt = 0; attempt < 1024; attempt++)
        {
            var option = SelectOption(options, totalWeight);
            var roll = GenerateRoll(option, itemLevel, round, contentRepository);
            if (existingKeys.Add(roll.Key))
            {
                return roll;
            }
        }

        return null;
    }

    private static EquipmentRandomAffixOptionDefinition SelectOption(
        IReadOnlyList<EquipmentRandomAffixOptionDefinition> options,
        int totalWeight)
    {
        while (true)
        {
            foreach (var option in options)
            {
                if (Probability.RollChance(option.Weight / (double)totalWeight))
                {
                    return option;
                }
            }
        }
    }

    private static GeneratedEquipmentAffixRoll GenerateRoll(
        EquipmentRandomAffixOptionDefinition option,
        int itemLevel,
        int round,
        IContentRepository contentRepository) =>
        option.Kind switch
        {
            EquipmentRandomAffixKind.AttackCombo => BuildAttackComboRoll(itemLevel, round),
            EquipmentRandomAffixKind.DefenceCombo => BuildDefenceComboRoll(option),
            EquipmentRandomAffixKind.RandomAttribute => BuildRandomAttributeRoll(itemLevel, round),
            EquipmentRandomAffixKind.Talent => BuildTalentRoll(option),
            EquipmentRandomAffixKind.Accuracy => BuildStatRangeRoll("accuracy", StatType.Accuracy, option),
            EquipmentRandomAffixKind.ExternalSkillBonus => BuildExternalSkillBonusRoll(itemLevel, round, contentRepository),
            EquipmentRandomAffixKind.InternalSkillBonus => BuildInternalSkillBonusRoll(itemLevel, round, contentRepository),
            EquipmentRandomAffixKind.FormSkillBonus => BuildFormSkillBonusRoll(itemLevel, round, contentRepository),
            EquipmentRandomAffixKind.LegendSkillBonus => BuildLegendSkillBonusRoll(itemLevel, round, contentRepository),
            EquipmentRandomAffixKind.CritChance => BuildDirectCritChanceRoll(itemLevel),
            EquipmentRandomAffixKind.CritMult => BuildStatRangeRoll("crit_mult", StatType.CritMult, option),
            EquipmentRandomAffixKind.Lifesteal => BuildStatRangeRoll("lifesteal", StatType.Lifesteal, option),
            EquipmentRandomAffixKind.Speed => BuildSpeedRoll(option),
            EquipmentRandomAffixKind.AntiDebuff => BuildStatRangeRoll("anti_debuff", StatType.AntiDebuff, option),
            EquipmentRandomAffixKind.WeaponBonus => BuildWeaponBonusRoll(option),
            _ => throw new InvalidOperationException($"Unsupported equipment random affix kind '{option.Kind}'."),
        };

    private static GeneratedEquipmentAffixRoll BuildAttackComboRoll(int itemLevel, int round)
    {
        var attackMin = itemLevel * (1 + round * 2);
        var attackMax = itemLevel * 2 * (1 + round * 2);
        var attack = Random.Shared.Next(attackMin, attackMax + 1);
        var critChance = Random.Shared.Next(1, itemLevel + 1);

        return new GeneratedEquipmentAffixRoll(
            "attack_combo",
            EquipmentRandomAffixKind.AttackCombo,
            [
                new StatModifierAffix(StatType.Attack, ModifierValue.Add(attack)),
                new StatModifierAffix(StatType.CritChance, ModifierValue.Add(AsRatio(critChance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildDefenceComboRoll(EquipmentRandomAffixOptionDefinition option)
    {
        var defence = RollRange(option, 0);
        var antiCritChance = RollRange(option, 1);

        return new GeneratedEquipmentAffixRoll(
            "defence_combo",
            EquipmentRandomAffixKind.DefenceCombo,
            [
                new StatModifierAffix(StatType.Defence, ModifierValue.Add(defence)),
                new StatModifierAffix(StatType.AntiCritChance, ModifierValue.Add(AsRatio(antiCritChance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildRandomAttributeRoll(int itemLevel, int round)
    {
        var stat = PickRandom(RandomAttributeStats);
        var min = (int)(itemLevel * ((round + 3) / 4d)) + itemLevel;
        var max = (int)(itemLevel * ((round + 3) / 3d)) + itemLevel * 2;
        var value = Random.Shared.Next(min, max + 1);
        var key = $"random_attribute:{stat}";

        return new GeneratedEquipmentAffixRoll(
            key,
            EquipmentRandomAffixKind.RandomAttribute,
            [new StatModifierAffix(stat, ModifierValue.Add(value))]);
    }

    private static GeneratedEquipmentAffixRoll BuildTalentRoll(EquipmentRandomAffixOptionDefinition option)
    {
        if (option.Pool.Count == 0)
        {
            throw new InvalidOperationException("Talent affix option requires a non-empty pool.");
        }

        var talentId = PickRandom(option.Pool);
        return new GeneratedEquipmentAffixRoll(
            $"talent:{talentId}",
            EquipmentRandomAffixKind.Talent,
            [new GrantTalentAffix(talentId)]);
    }

    private static GeneratedEquipmentAffixRoll BuildExternalSkillBonusRoll(
        int itemLevel,
        int round,
        IContentRepository contentRepository)
    {
        var candidates = contentRepository.GetExternalSkills()
            .Where(skill => IsHardMatch(skill.Hard, itemLevel))
            .ToArray();
        var skill = PickRandom(candidates);
        var value = RollSkillBonusValue(
            skill.Hard,
            round,
            minFactor: 3d,
            maxFactor: 15d,
            easyTierBonusFactor: 15d);

        return new GeneratedEquipmentAffixRoll(
            $"external_skill_bonus:{skill.Id}",
            EquipmentRandomAffixKind.ExternalSkillBonus,
            [new SkillBonusModifierAffix(skill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildInternalSkillBonusRoll(
        int itemLevel,
        int round,
        IContentRepository contentRepository)
    {
        var candidates = contentRepository.GetInternalSkills()
            .Where(skill => IsHardMatch(skill.Hard, itemLevel))
            .ToArray();
        var skill = PickRandom(candidates);
        var value = RollSkillBonusValue(
            skill.Hard,
            round,
            minFactor: 2d,
            maxFactor: 10d,
            easyTierBonusFactor: 10d);

        return new GeneratedEquipmentAffixRoll(
            $"internal_skill_bonus:{skill.Id}",
            EquipmentRandomAffixKind.InternalSkillBonus,
            [new SkillBonusModifierAffix(skill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildFormSkillBonusRoll(
        int itemLevel,
        int round,
        IContentRepository contentRepository)
    {
        var candidates = contentRepository.GetExternalSkills()
            .SelectMany(static skill => skill.FormSkills)
            .Concat(contentRepository.GetInternalSkills().SelectMany(static skill => skill.FormSkills))
            .Where(formSkill => IsHardMatch(formSkill.Hard, itemLevel))
            .ToArray();
        var formSkill = PickRandom(candidates);
        var value = RollSkillBonusValue(
            formSkill.Hard,
            round,
            minFactor: 10d,
            maxFactor: 25d,
            easyTierBonusFactor: 15d);

        return new GeneratedEquipmentAffixRoll(
            $"form_skill_bonus:{formSkill.Id}",
            EquipmentRandomAffixKind.FormSkillBonus,
            [new SkillBonusModifierAffix(formSkill.Id, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildLegendSkillBonusRoll(
        int itemLevel,
        int round,
        IContentRepository contentRepository)
    {
        var candidates = contentRepository.GetLegendSkills()
            .Select(skill => (Skill: skill, Hard: ResolveLegendSkillHard(skill, contentRepository)))
            .Where(entry => entry.Hard is not null && IsHardMatch(entry.Hard.Value, itemLevel))
            .ToArray();
        var candidate = PickRandom(candidates);
        var power = RollSkillBonusValue(
            candidate.Hard!.Value,
            round,
            minFactor: 15d,
            maxFactor: 30d,
            easyTierBonusFactor: 15d);
        var chance = Random.Shared.Next(0, 11);

        return new GeneratedEquipmentAffixRoll(
            $"legend_skill_bonus:{candidate.Skill.Id}",
            EquipmentRandomAffixKind.LegendSkillBonus,
            [
                new SkillBonusModifierAffix(candidate.Skill.Id, ModifierValue.Add(AsRatio(power))),
                new LegendSkillChanceModifierAffix(candidate.Skill.Id, ModifierValue.Add(AsRatio(chance))),
            ]);
    }

    private static GeneratedEquipmentAffixRoll BuildDirectCritChanceRoll(int itemLevel)
    {
        var percent = Math.Round(Random.Shared.NextDouble() * (itemLevel - 0.5d) + 0.5d, 2);
        return new GeneratedEquipmentAffixRoll(
            "crit_chance",
            EquipmentRandomAffixKind.CritChance,
            [new StatModifierAffix(StatType.CritChance, ModifierValue.Add(percent / 100d))]);
    }

    private static GeneratedEquipmentAffixRoll BuildSpeedRoll(EquipmentRandomAffixOptionDefinition option)
    {
        if (option.Pool.Count == 0)
        {
            throw new InvalidOperationException("Speed affix option requires a non-empty value pool.");
        }

        var value = double.Parse(PickRandom(option.Pool), CultureInfo.InvariantCulture);
        return new GeneratedEquipmentAffixRoll(
            "speed",
            EquipmentRandomAffixKind.Speed,
            [new StatModifierAffix(StatType.Speed, ModifierValue.Add(value))]);
    }

    private static GeneratedEquipmentAffixRoll BuildWeaponBonusRoll(EquipmentRandomAffixOptionDefinition option)
    {
        if (option.WeaponType is null)
        {
            throw new InvalidOperationException("Weapon bonus affix option requires weaponType.");
        }

        var value = RollRange(option, 0);
        return new GeneratedEquipmentAffixRoll(
            $"weapon_bonus:{option.WeaponType.Value}",
            EquipmentRandomAffixKind.WeaponBonus,
            [new WeaponBonusModifierAffix(option.WeaponType.Value, ModifierValue.Add(AsRatio(value)))]);
    }

    private static GeneratedEquipmentAffixRoll BuildStatRangeRoll(
        string key,
        StatType stat,
        EquipmentRandomAffixOptionDefinition option)
    {
        var value = RollRange(option, 0);
        var delta = stat is StatType.Accuracy or StatType.CritMult or StatType.Lifesteal or StatType.AntiDebuff
            ? AsRatio(value)
            : value;

        return new GeneratedEquipmentAffixRoll(
            key,
            option.Kind,
            [new StatModifierAffix(stat, ModifierValue.Add(delta))]);
    }

    private static int RollRange(EquipmentRandomAffixOptionDefinition option, int index)
    {
        if (option.Ranges.Count <= index)
        {
            throw new InvalidOperationException(
                $"Equipment random affix option '{option.Kind}' is missing range index {index}.");
        }

        var range = option.Ranges[index];
        return Random.Shared.Next(range.Min, range.Max + 1);
    }

    private static int RollSkillBonusValue(
        double hard,
        int round,
        double minFactor,
        double maxFactor,
        double easyTierBonusFactor)
    {
        var scale = (round + 3d) / (hard / 2d + 1d);
        var min = (int)(minFactor * scale);
        var max = (int)(maxFactor * scale);
        if (hard < 6d)
        {
            max += (int)(round * easyTierBonusFactor);
        }

        return Random.Shared.Next(min, max + 1);
    }

    private static bool IsHardMatch(double hard, int itemLevel) =>
        itemLevel + 4d >= hard && hard + 1d >= itemLevel;

    private static double? ResolveLegendSkillHard(
        LegendSkillDefinition skill,
        IContentRepository contentRepository)
    {
        if (!contentRepository.TryGetExternalSkill(skill.StartSkill, out var startSkill))
        {
            return null;
        }

        return startSkill.Hard;
    }

    private static int RollEquipmentAffixCount()
    {
        if (Probability.RollChance(0.1d))
        {
            return 4;
        }

        if (Probability.RollChance(0.2d))
        {
            return 3;
        }

        return Probability.RollChance(0.4d) ? 2 : 1;
    }

    private static T PickRandom<T>(IReadOnlyList<T> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Random selection candidates cannot be empty.");
        }

        return values[Random.Shared.Next(values.Count)];
    }

    private static double AsRatio(int value) => value / 100d;
}
