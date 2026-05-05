using Game.Content.Loading;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Core.Story;
using SkillAffixDefinition = Game.Core.Affix.SkillAffixDefinition;

namespace Game.Tests;

internal static class TestContentFactory
{
    public static CharacterInstance CreateCharacterInstance(string id, CharacterDefinition definition) =>
        CharacterMapper.CreateInitial(id, definition, new EquipmentInstanceFactory());

    public static CharacterInstance CreateCharacterInstance(
        string id,
        CharacterDefinition definition,
        EquipmentInstanceFactory equipmentInstanceFactory) =>
        CharacterMapper.CreateInitial(id, definition, equipmentInstanceFactory);

    public static ExternalSkillDefinition CreateExternalSkill(
        string id,
        int mpCost = 0,
        int rageCost = 0,
        int cooldown = 0,
        IReadOnlyList<SkillBuffDefinition>? buffs = null,
        IReadOnlyList<FormSkillDefinition>? formSkills = null,
        double powerBase = 0d,
        double powerStep = 0d,
        string description = "",
        string icon = "",
        WeaponType type = WeaponType.Quanzhang,
        bool isHarmony = false,
        double affinity = 0d,
        double hard = 1d,
        SkillImpactType? impactType = null,
        int? impactSize = null,
        int? castSize = null,
        string audio = "",
        string animation = "",
        IReadOnlyList<ExternalSkillLevelDefinition>? levelOverrides = null,
        IReadOnlyList<SkillAffixDefinition>? affixes = null) =>
        new()
        {
            Id = id,
            Name = id,
            Description = description,
            Icon = icon,
            Type = type,
            IsHarmony = isHarmony,
            Affinity = affinity,
            Hard = hard,
            Cooldown = cooldown,
            Cost = new SkillCostDefinition(mpCost, rageCost),
            Targeting = new SkillTargetingDefinition(CastSize: castSize, ImpactType: impactType, ImpactSize: impactSize),
            PowerBase = powerBase,
            PowerStep = powerStep,
            Audio = audio,
            Animation = animation,
            Buffs = buffs ?? [],
            RawLevelOverrides = levelOverrides ?? [],
            FormSkills = formSkills ?? [],
            Affixes = affixes ?? [],
        };

    public static InternalSkillDefinition CreateInternalSkill(
        string id,
        string description = "",
        string icon = "",
        int yin = 0,
        int yang = 0,
        double attackScale = 0d,
        double criticalScale = 0d,
        double defenceScale = 0d,
        double hard = 1d,
        IReadOnlyList<FormSkillDefinition>? formSkills = null,
        IReadOnlyList<SkillAffixDefinition>? affixes = null) =>
        new()
        {
            Id = id,
            Name = id,
            Description = description,
            Icon = icon,
            Yin = yin,
            Yang = yang,
            AttackScale = attackScale,
            CriticalScale = criticalScale,
            DefenceScale = defenceScale,
            Hard = hard,
            FormSkills = formSkills ?? [],
            Affixes = affixes ?? [],
        };

    public static CharacterDefinition CreateCharacterDefinition(
        string id,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null,
        IReadOnlyList<InitialInternalSkillEntryDefinition>? internalSkills = null,
        IReadOnlyList<TalentDefinition>? talents = null,
        IReadOnlyList<EquipmentDefinition>? equipment = null,
        int level = 1,
        string? portrait = null,
        string? model = null,
        CharacterGender gender = CharacterGender.Neutral,
        string? growTemplate = null,
        bool arenaEnabled = false,
        IReadOnlyList<SpecialSkillDefinition>? specialSkills = null)
    {
        var definition = new CharacterDefinition(
            id,
            id,
            stats ?? new Dictionary<StatType, int>(),
            externalSkills ?? [],
            internalSkills ?? [],
            (talents ?? []).Select(static talent => talent.Id).ToList(),
            (equipment ?? []).Select(static item => item.Id).ToList(),
            level,
            portrait,
            model,
            gender,
            growTemplate,
            arenaEnabled,
            (specialSkills ?? []).Select(static skill => skill.Id).ToList());

        definition.Resolve(CreateRepository(
            externalSkills: (externalSkills ?? []).Select(static entry => entry.Skill),
            internalSkills: (internalSkills ?? []).Select(static entry => entry.Skill),
            specialSkills: specialSkills ?? [],
            talents: talents ?? [],
            equipment: equipment ?? []));

        return definition;
    }

    public static InMemoryContentRepository CreateRepository(
        IEnumerable<CharacterDefinition>? characters = null,
        IEnumerable<ExternalSkillDefinition>? externalSkills = null,
        IEnumerable<InternalSkillDefinition>? internalSkills = null,
        IEnumerable<GrowTemplateDefinition>? growTemplates = null,
        IEnumerable<LegendSkillDefinition>? legendSkills = null,
        IEnumerable<SpecialSkillDefinition>? specialSkills = null,
        IEnumerable<StoryScript>? storyScripts = null,
        IEnumerable<TalentDefinition>? talents = null,
        IEnumerable<EquipmentDefinition>? equipment = null,
        IEnumerable<ItemDefinition>? items = null,
        IEnumerable<BuffDefinition>? buffs = null,
        IEnumerable<MapDefinition>? maps = null,
        IEnumerable<ResourceDefinition>? resources = null,
        IEnumerable<ShopDefinition>? shops = null) =>
        CreateRepositoryInternal(
            characters,
            externalSkills,
            internalSkills,
            growTemplates,
            legendSkills,
            specialSkills,
            storyScripts,
            talents,
            equipment,
            items,
            buffs,
            maps,
            resources,
            shops);

    private static InMemoryContentRepository CreateRepositoryInternal(
        IEnumerable<CharacterDefinition>? characters,
        IEnumerable<ExternalSkillDefinition>? externalSkills,
        IEnumerable<InternalSkillDefinition>? internalSkills,
        IEnumerable<GrowTemplateDefinition>? growTemplates,
        IEnumerable<LegendSkillDefinition>? legendSkills,
        IEnumerable<SpecialSkillDefinition>? specialSkills,
        IEnumerable<StoryScript>? storyScripts,
        IEnumerable<TalentDefinition>? talents,
        IEnumerable<EquipmentDefinition>? equipment,
        IEnumerable<ItemDefinition>? items,
        IEnumerable<BuffDefinition>? buffs,
        IEnumerable<MapDefinition>? maps,
        IEnumerable<ResourceDefinition>? resources,
        IEnumerable<ShopDefinition>? shops)
    {
        var storyScriptMap = (storyScripts ?? [])
            .Select((script, index) => (Key: $"story_{index}", Script: script))
            .ToDictionary(entry => entry.Key, entry => entry.Script, StringComparer.Ordinal);
        var storySegments = new Dictionary<string, StorySegmentEntry>(StringComparer.Ordinal);
        var combinedStoryScript = new StoryScript(
            storyScriptMap.Values.FirstOrDefault()?.Version ?? 1,
            storyScriptMap.Values.SelectMany(static script => script.Segments).ToList());
        foreach (var (scriptId, script) in storyScriptMap)
        {
            foreach (var segment in script.Segments)
            {
                storySegments.Add(segment.Name, new StorySegmentEntry(segment.Name, scriptId, combinedStoryScript, segment));
            }
        }

        return new InMemoryContentRepository
        {
            Battles = new Dictionary<string, BattleDefinition>(StringComparer.Ordinal),
            Characters = (characters ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            ExternalSkills = (externalSkills ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            GameTips = new Dictionary<string, GameTipDefinition>(StringComparer.Ordinal),
            GrowTemplates = (growTemplates ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            InternalSkills = (internalSkills ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            LegendSkills = (legendSkills ?? []).ToList(),
            Maps = (maps ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Resources = (resources ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Sects = new OrderedDictionary<string, SectDefinition>(StringComparer.Ordinal),
            Shops = (shops ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            SpecialSkills = (specialSkills ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            StoryScripts = storyScriptMap,
            StorySegments = storySegments,
            Items = (items ?? []).Concat(equipment ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Buffs = (buffs ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Talents = (talents ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Equipments = (equipment ?? []).ToDictionary(definition => definition.Id, StringComparer.Ordinal),
            Towers = new Dictionary<string, TowerDefinition>(StringComparer.Ordinal),
        };
    }

    public static GrowTemplateDefinition CreateGrowTemplate(
        string id,
        IReadOnlyDictionary<StatType, int>? statGrowth = null) =>
        new()
        {
            Id = id,
            Name = id,
            StatGrowth = statGrowth ?? new Dictionary<StatType, int>(),
        };

    public static EquipmentDefinition CreateEquipment(
        string id,
        EquipmentSlotType slotType = EquipmentSlotType.Weapon) =>
        new()
        {
            Id = id,
            Name = id,
            Type = ItemType.Equipment,
            SlotType = slotType,
        };
}
