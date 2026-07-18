using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

internal sealed class ZhenlongqijuBattleFactory(GameSession session)
{
    private GameState State => session.State;
    private GameConfig Config => session.Config;
    private IContentRepository Content => session.ContentRepository;

    public IReadOnlyList<OrdinaryBattleRewardDrop> GenerateDrops(int level)
    {
        var drops = new List<OrdinaryBattleRewardDrop>();
        AddRandomExternalSkillFragment(drops, skill => skill.Hard < 8d);
        if (Random.Shared.NextDouble() < 0.5d)
        {
            AddRandomExternalSkillFragment(drops, skill => skill.Hard < 8d);
        }

        if (Enumerable.Range(0, level).Any(_ => Random.Shared.NextDouble() < 0.3d))
        {
            AddRandomExternalSkillFragment(drops, skill => skill.Hard >= 8d);
        }

        if (Enumerable.Range(0, level).Any(_ => Random.Shared.NextDouble() < 0.3d))
        {
            var equipment = PickConfiguredEquipment();
            drops.Add(new OrdinaryBattleEquipmentRewardDrop(
                equipment,
                EquipmentRandomAffixGenerator.GenerateRolls(
                    equipment,
                    Content,
                    State.Adventure.Round,
                    4)));
        }

        return drops;
    }

    public void PowerUpEnemy(CharacterInstance character, int level, EquipmentInstanceFactory equipmentFactory)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(equipmentFactory);
        if (level <= 0) return;

        var maxResourceBonus = checked(level * 2000);
        var resourceMultiplier = (int)(1.0d + level * 0.1d);
        ScaleResource(character, StatType.MaxHp, resourceMultiplier, maxResourceBonus);
        ScaleResource(character, StatType.MaxMp, resourceMultiplier, maxResourceBonus);

        foreach (var stat in CoreStats)
        {
            character.AddBaseStat(stat, Random.Shared.Next(level * 2, level * 4 + 1));
        }

        foreach (var skill in character.ExternalSkills)
            skill.Level += Random.Shared.Next(level / 5, level / 3 + 1);
        foreach (var skill in character.InternalSkills)
            skill.Level += Random.Shared.Next(level / 5, level / 3 + 1);

        EquipEnemy(character, equipmentFactory);
        character.RebuildSnapshot();
    }

    private static readonly StatType[] CoreStats =
    [
        StatType.Bili, StatType.Shenfa, StatType.Gengu, StatType.Dingli, StatType.Fuyuan,
        StatType.Wuxing, StatType.Quanzhang, StatType.Daofa, StatType.Jianfa, StatType.Qimen,
    ];

    private static void ScaleResource(CharacterInstance character, StatType stat, int multiplier, int bonus)
    {
        var current = character.GetBaseStat(stat);
        character.AddBaseStat(stat, checked(current * multiplier + bonus - current));
    }

    private void AddRandomExternalSkillFragment(
        ICollection<OrdinaryBattleRewardDrop> drops,
        Func<ExternalSkillDefinition, bool> predicate)
    {
        var candidates = Content.GetExternalSkills()
            .Where(predicate)
            .Where(skill => session.SkillMaxLevelPolicy.GetExternalSkillMaxLevelWithoutRoundBonus(skill.Id) < Config.AbsoluteSkillMaxLevel)
            .ToArray();
        if (candidates.Length == 0) return;
        var skill = PickRandom(candidates);
        drops.Add(new OrdinaryBattleSkillFragmentRewardDrop(
            SkillFragmentKind.External, skill.Id, $"{skill.Name}残章"));
    }

    private void EquipEnemy(CharacterInstance character, EquipmentInstanceFactory equipmentFactory)
    {
        character.EquippedItems.Clear();
        foreach (var slot in new[] { EquipmentSlotType.Weapon, EquipmentSlotType.Armor, EquipmentSlotType.Accessory })
        {
            var equipment = PickConfiguredEquipment(slot);
            var affixes = EquipmentRandomAffixGenerator
                .GenerateRolls(equipment, Content, State.Adventure.Round, 4)
                .SelectMany(static roll => roll.Affixes)
                .ToArray();
            character.AddEquipmentInstance(equipmentFactory.Create(equipment, affixes));
        }
    }

    private EquipmentDefinition PickConfiguredEquipment()
    {
        var candidates = Config.ZhenlongWeaponRewardIds
            .Concat(Config.ZhenlongArmorRewardIds)
            .Concat(Config.ZhenlongAccessoryRewardIds)
            .Distinct(StringComparer.Ordinal)
            .Select(ResolveEquipment)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException(
                "Zhenlongqiju equipment reward requires at least one configured equipment definition.");
        return PickRandom(candidates);
    }

    private EquipmentDefinition PickConfiguredEquipment(EquipmentSlotType slot)
    {
        var ids = slot switch
        {
            EquipmentSlotType.Weapon => Config.ZhenlongWeaponRewardIds,
            EquipmentSlotType.Armor => Config.ZhenlongArmorRewardIds,
            EquipmentSlotType.Accessory => Config.ZhenlongAccessoryRewardIds,
            _ => throw new InvalidOperationException($"Unsupported zhenlongqiju equipment slot '{slot}'."),
        };
        var candidates = ids.Distinct(StringComparer.Ordinal).Select(id => ResolveEquipment(id, slot)).ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException(
                $"Zhenlongqiju enemy equipment requires at least one configured {slot} equipment definition.");
        return PickRandom(candidates);
    }

    private EquipmentDefinition ResolveEquipment(string id) =>
        Content.GetItem(RequireId(id)) as EquipmentDefinition
        ?? throw new InvalidOperationException($"Zhenlongqiju equipment reward '{id}' is not an equipment definition.");

    private EquipmentDefinition ResolveEquipment(string id, EquipmentSlotType slot)
    {
        var equipment = ResolveEquipment(id);
        return equipment.SlotType == slot
            ? equipment
            : throw new InvalidOperationException(
                $"Zhenlongqiju enemy equipment '{equipment.Id}' must be a {slot} equipment definition.");
    }

    private static string RequireId(string id) =>
        !string.IsNullOrWhiteSpace(id)
            ? id.Trim()
            : throw new InvalidOperationException("Zhenlongqiju equipment id cannot be empty.");

    private static T PickRandom<T>(IReadOnlyList<T> items) =>
        items.Count > 0
            ? items[Random.Shared.Next(items.Count)]
            : throw new InvalidOperationException("Cannot pick a random item from an empty list.");
}
