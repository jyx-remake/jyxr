using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed class ContentPackage
{
    public List<BattleDefinition> Battles { get; init; } = [];
    public List<CharacterDefinition> Characters { get; init; } = [];
    public List<ExternalSkillDefinition> ExternalSkills { get; init; } = [];
    public List<GameTipDefinition> GameTips { get; init; } = [];
    public List<GrowTemplateDefinition> GrowTemplates { get; init; } = [];
    public List<InternalSkillDefinition> InternalSkills { get; init; } = [];
    public List<LegendSkillDefinition> LegendSkills { get; init; } = [];
    public List<MapDefinition> Maps { get; init; } = [];
    public List<ResourceDefinition> Resources { get; init; } = [];
    public List<SectDefinition> Sects { get; init; } = [];
    public List<ShopDefinition> Shops { get; init; } = [];
    public List<SpecialSkillDefinition> SpecialSkills { get; init; } = [];
    public Dictionary<string, StoryScript> StoryScripts { get; init; } = new(StringComparer.Ordinal);
    public List<ItemDefinition> Items { get; init; } = [];
    public List<BuffDefinition> Buffs { get; init; } = [];
    public List<TalentDefinition> Talents { get; init; } = [];
    public List<TowerDefinition> Towers { get; init; } = [];
}
