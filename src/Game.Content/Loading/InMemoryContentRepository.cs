using System.Diagnostics.CodeAnalysis;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed class InMemoryContentRepository : IContentRepository
{
	public required Dictionary<string, BattleDefinition> Battles { get; init; }
	public required Dictionary<string, CharacterDefinition> Characters { get; init; }
	public required Dictionary<string, ExternalSkillDefinition> ExternalSkills { get; init; }
	public required Dictionary<string, GameTipDefinition> GameTips { get; init; }
	public required Dictionary<string, GrowTemplateDefinition> GrowTemplates { get; init; }
	public required Dictionary<string, InternalSkillDefinition> InternalSkills { get; init; }
	public required Dictionary<string, MapDefinition> Maps { get; init; }
	public required List<WorldTriggerDefinition> WorldTriggers { get; init; }
	public required Dictionary<string, ResourceDefinition> Resources { get; init; }
	public required OrderedDictionary<string, SectDefinition> Sects { get; init; }
	public required Dictionary<string, ShopDefinition> Shops { get; init; }
	public required Dictionary<string, SpecialSkillDefinition> SpecialSkills { get; init; }
	public required Dictionary<string, StoryScript> StoryScripts { get; init; }
	public required Dictionary<string, StorySegmentEntry> StorySegments { get; init; }
	public required Dictionary<string, ItemDefinition> Items { get; init; }
	public required List<EquipmentRandomAffixTableDefinition> EquipmentRandomAffixTables { get; init; }
	public required Dictionary<string, BuffDefinition> Buffs { get; init; }
	public required Dictionary<string, TalentDefinition> Talents { get; init; }
	public required Dictionary<string, EquipmentDefinition> Equipments { get; init; }
	public required List<LegendSkillDefinition> LegendSkills { get; init; }
	public required Dictionary<string, TowerDefinition> Towers { get; init; }

	public BattleDefinition GetBattle(string id) => Battles[id];
	public bool TryGetBattle(string id, [NotNullWhen(true)] out BattleDefinition? definition) =>
		Battles.TryGetValue(id, out definition);
	public IReadOnlyCollection<BattleDefinition> GetBattles() => Battles.Values;

	public CharacterDefinition GetCharacter(string id) => Characters[id];
	public bool TryGetCharacter(string id, [NotNullWhen(true)] out CharacterDefinition? definition) =>
		Characters.TryGetValue(id, out definition);
	public IReadOnlyCollection<CharacterDefinition> GetCharacters() => Characters.Values;
	public IReadOnlyCollection<ItemDefinition> GetItems() => Items.Values;

	public ExternalSkillDefinition GetExternalSkill(string id) => ExternalSkills[id];
	public bool TryGetExternalSkill(string id, [NotNullWhen(true)] out ExternalSkillDefinition? definition) =>
		ExternalSkills.TryGetValue(id, out definition);
	public IReadOnlyCollection<ExternalSkillDefinition> GetExternalSkills() => ExternalSkills.Values;

	public GameTipDefinition GetGameTip(string id) => GameTips[id];
	public bool TryGetGameTip(string id, [NotNullWhen(true)] out GameTipDefinition? definition) =>
		GameTips.TryGetValue(id, out definition);

	public GrowTemplateDefinition GetGrowTemplate(string id) => GrowTemplates[id];
	public bool TryGetGrowTemplate(string id, [NotNullWhen(true)] out GrowTemplateDefinition? definition) =>
		GrowTemplates.TryGetValue(id, out definition);

	public InternalSkillDefinition GetInternalSkill(string id) => InternalSkills[id];
	public bool TryGetInternalSkill(string id, [NotNullWhen(true)] out InternalSkillDefinition? definition) =>
		InternalSkills.TryGetValue(id, out definition);
	public IReadOnlyCollection<InternalSkillDefinition> GetInternalSkills() => InternalSkills.Values;

	public MapDefinition GetMap(string id) => Maps[id];
	public bool TryGetMap(string id, [NotNullWhen(true)] out MapDefinition? definition) =>
		Maps.TryGetValue(id, out definition);

	public IReadOnlyList<WorldTriggerDefinition> GetWorldTriggers() => WorldTriggers;

	public ResourceDefinition GetResource(string id) => Resources[id];
	public bool TryGetResource(string id, [NotNullWhen(true)] out ResourceDefinition? definition) =>
		Resources.TryGetValue(id, out definition);

	public SectDefinition GetSect(string id) => Sects[id];
	public bool TryGetSect(string id, [NotNullWhen(true)] out SectDefinition? definition) =>
		Sects.TryGetValue(id, out definition);

	public ShopDefinition GetShop(string id) => Shops[id];
	public bool TryGetShop(string id, [NotNullWhen(true)] out ShopDefinition? definition) =>
		Shops.TryGetValue(id, out definition);

	public SpecialSkillDefinition GetSpecialSkill(string id) => SpecialSkills[id];
	public bool TryGetSpecialSkill(string id, [NotNullWhen(true)] out SpecialSkillDefinition? definition) =>
		SpecialSkills.TryGetValue(id, out definition);

	public StoryScript GetStoryScript(string id) => StoryScripts[id];
	public bool TryGetStoryScript(string id, [NotNullWhen(true)] out StoryScript? script) =>
		StoryScripts.TryGetValue(id, out script);

	public StorySegmentEntry GetStorySegment(string id) => StorySegments[id];
	public bool TryGetStorySegment(string id, [NotNullWhen(true)] out StorySegmentEntry? entry) =>
		StorySegments.TryGetValue(id, out entry);

	public ItemDefinition GetItem(string id) => Items[id];
	public bool TryGetItem(string id, [NotNullWhen(true)] out ItemDefinition? definition) =>
		Items.TryGetValue(id, out definition);

	public BuffDefinition GetBuff(string id) => Buffs[id];
	public bool TryGetBuff(string id, [NotNullWhen(true)] out BuffDefinition? definition) =>
		Buffs.TryGetValue(id, out definition);

	public TalentDefinition GetTalent(string id) => Talents[id];
	public bool TryGetTalent(string id, [NotNullWhen(true)] out TalentDefinition? definition) =>
		Talents.TryGetValue(id, out definition);

	public EquipmentDefinition GetEquipment(string id) => Equipments[id];
	public bool TryGetEquipment(string id, [NotNullWhen(true)] out EquipmentDefinition? definition) =>
		Equipments.TryGetValue(id, out definition);
	public IReadOnlyList<EquipmentRandomAffixTableDefinition> GetEquipmentRandomAffixTables() => EquipmentRandomAffixTables;

	public IReadOnlyList<LegendSkillDefinition> GetLegendSkills() => LegendSkills;
	public IReadOnlyList<ResourceDefinition> GetResourcesByGroup(string group)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(group);

		return Resources.Values
			.Where(resource => string.Equals(resource.Group, group, StringComparison.Ordinal))
			.ToList();
	}

	public TowerDefinition GetTower(string id) => Towers[id];
	public bool TryGetTower(string id, [NotNullWhen(true)] out TowerDefinition? definition) =>
		Towers.TryGetValue(id, out definition);
	public IReadOnlyCollection<TowerDefinition> GetTowers() => Towers.Values;
}
