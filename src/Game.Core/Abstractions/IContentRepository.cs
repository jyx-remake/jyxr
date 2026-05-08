using System.Diagnostics.CodeAnalysis;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Story;

namespace Game.Core.Abstractions;

public interface IContentRepository
{
    OrderedDictionary<string, SectDefinition> Sects { get; }

    BattleDefinition GetBattle(string id);
    bool TryGetBattle(string id, [NotNullWhen(true)] out BattleDefinition? definition);
    CharacterDefinition GetCharacter(string id);
    bool TryGetCharacter(string id, [NotNullWhen(true)] out CharacterDefinition? definition);
    ExternalSkillDefinition GetExternalSkill(string id);
    bool TryGetExternalSkill(string id, [NotNullWhen(true)] out ExternalSkillDefinition? definition);
    GameTipDefinition GetGameTip(string id);
    bool TryGetGameTip(string id, [NotNullWhen(true)] out GameTipDefinition? definition);
    GrowTemplateDefinition GetGrowTemplate(string id);
    bool TryGetGrowTemplate(string id, [NotNullWhen(true)] out GrowTemplateDefinition? definition);
    InternalSkillDefinition GetInternalSkill(string id);
    bool TryGetInternalSkill(string id, [NotNullWhen(true)] out InternalSkillDefinition? definition);
    MapDefinition GetMap(string id);
    bool TryGetMap(string id, [NotNullWhen(true)] out MapDefinition? definition);
    IReadOnlyList<WorldTriggerDefinition> GetWorldTriggers();
    ResourceDefinition GetResource(string id);
    bool TryGetResource(string id, [NotNullWhen(true)] out ResourceDefinition? definition);
    SectDefinition GetSect(string id);
    bool TryGetSect(string id, [NotNullWhen(true)] out SectDefinition? definition);
    ShopDefinition GetShop(string id);
    bool TryGetShop(string id, [NotNullWhen(true)] out ShopDefinition? definition);
    SpecialSkillDefinition GetSpecialSkill(string id);
    bool TryGetSpecialSkill(string id, [NotNullWhen(true)] out SpecialSkillDefinition? definition);
    StoryScript GetStoryScript(string id);
    bool TryGetStoryScript(string id, [NotNullWhen(true)] out StoryScript? script);
    StorySegmentEntry GetStorySegment(string id);
    bool TryGetStorySegment(string id, [NotNullWhen(true)] out StorySegmentEntry? entry);
    ItemDefinition GetItem(string id);
    bool TryGetItem(string id, [NotNullWhen(true)] out ItemDefinition? definition);
    BuffDefinition GetBuff(string id);
    bool TryGetBuff(string id, [NotNullWhen(true)] out BuffDefinition? definition);
    TalentDefinition GetTalent(string id);
    bool TryGetTalent(string id, [NotNullWhen(true)] out TalentDefinition? definition);
    EquipmentDefinition GetEquipment(string id);
    bool TryGetEquipment(string id, [NotNullWhen(true)] out EquipmentDefinition? definition);
    IReadOnlyList<LegendSkillDefinition> GetLegendSkills();
    IReadOnlyList<ResourceDefinition> GetResourcesByGroup(string group);
    TowerDefinition GetTower(string id);
    bool TryGetTower(string id, [NotNullWhen(true)] out TowerDefinition? definition);
}
