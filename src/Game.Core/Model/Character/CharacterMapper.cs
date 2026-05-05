using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model.Skills;
using Game.Core.Persistence;

namespace Game.Core.Model.Character;

public static class CharacterMapper
{
    public static CharacterInstance CreateInitial(
        string id,
        CharacterDefinition definition,
        EquipmentInstanceFactory equipmentInstanceFactory)
    {
        ArgumentNullException.ThrowIfNull(equipmentInstanceFactory);
        var character = new CharacterInstance
        {
            Id = id,
            Definition = definition,
            Name = definition.Name,
            Portrait = definition.Portrait,
            Model = definition.Model,
            GrowTemplateId = definition.GrowTemplate
        };
        character.SetLevel(definition.Level);
        CopyStats(definition.Stats, character.BaseStats);

        EnsureSingleEquippedInternalSkill(definition.InternalSkills.Count(skill => skill.Equipped));
        character.ExternalSkills.AddRange(definition.ExternalSkills.Select(skill =>
            new ExternalSkillInstance(skill.Skill, skill.Level, 0, true, character)));
        character.InternalSkills.AddRange(definition.InternalSkills.Select(skill =>
            new InternalSkillInstance(skill.Skill, skill.Level, 0, character)));

        var equippedInternalSkill = definition.InternalSkills.FirstOrDefault(skill => skill.Equipped);
        if (equippedInternalSkill is not null)
        {
            character.EquipInternalSkill(equippedInternalSkill.Skill.Id);
        }

        character.UnlockedTalents.AddRange(definition.Talents);
        character.SpecialSkills.AddRange(definition.SpecialSkills.Select(skill => new SpecialSkillInstance(skill, true, character)));
        foreach (var equipmentDefinition in definition.Equipments)
        {
            character.AddEquipmentInstance(equipmentInstanceFactory.Create(equipmentDefinition));
        }

        character.RebuildSnapshot();
        return character;
    }

    public static CharacterInstance FromRecord(CharacterRecord record, IContentRepository contentRepository)
    {
        var character = new CharacterInstance
        {
            Id = record.Id,
            Definition = contentRepository.GetCharacter(record.DefinitionId),
            Name = record.Name,
            Portrait = record.Portrait,
            Model = record.Model,
            GrowTemplateId = record.GrowTemplateId,
        };
        
        character.SetLevel(record.Level);
        character.GrantExperience(record.Experience);
        character.SetUnspentStatPoints(record.UnspentStatPoints);
        CopyStats(record.BaseStats, character.BaseStats);

        EnsureSingleEquippedInternalSkill(record.InternalSkills.Count(skill => skill.Equipped));
        character.UnlockedTalents.AddRange(record.UnlockedTalentIds.Select(contentRepository.GetTalent));
        character.SpecialSkills.AddRange(record.SpecialSkills.Select(skill =>
            new SpecialSkillInstance(
                contentRepository.GetSpecialSkill(skill.SpecialSkillDefinitionId),
                skill.IsActive,
                character)));
        character.ExternalSkills.AddRange(record.ExternalSkills.Select(skill =>
            new ExternalSkillInstance(
                contentRepository.GetExternalSkill(skill.ExternalSkillDefinitionId),
                skill.Level,
                skill.Exp,
                skill.IsActive,
                character)));
        character.InternalSkills.AddRange(record.InternalSkills.Select(skill =>
            new InternalSkillInstance(
                contentRepository.GetInternalSkill(skill.InternalSkillDefinitionId),
                skill.Level,
                skill.Exp,
                character)));

        var equippedInternalSkill = record.InternalSkills.FirstOrDefault(skill => skill.Equipped);
        if (equippedInternalSkill is not null)
        {
            character.EquipInternalSkill(equippedInternalSkill.InternalSkillDefinitionId);
        }

        foreach (var equipment in record.EquippedItems)
        {
            character.AddEquipmentInstance(EquipmentMapper.FromRecord(equipment.Value, contentRepository));
        }

        character.RebuildSnapshot();
        return character;
    }

    public static CharacterRecord ToRecord(CharacterInstance character) =>
        new(
            character.Id,
            character.Definition.Id,
            character.Name,
            character.Portrait,
            character.Model,
            character.GrowTemplateId,
            character.Level,
            character.Experience,
            character.UnspentStatPoints,
            new Dictionary<StatType, int>(character.BaseStats),
            character.UnlockedTalents.Select(talent => talent.Id).ToList(),
            character.SpecialSkills.Select(skill => new SpecialSkillRecord(skill.Definition.Id, skill.IsActive)).ToList(),
            character.ExternalSkills.Select(skill => new ExternalSkillRecord(skill.Definition.Id, skill.Level, skill.Exp, skill.IsActive)).ToList(),
            character.InternalSkills.Select(skill => new InternalSkillRecord(skill.Definition.Id, skill.Level, skill.Exp, skill.IsEquipped)).ToList(),
            character.EquippedItems.ToDictionary(
                entry => entry.Key,
                entry => EquipmentMapper.ToRecord(entry.Value)));

    private static void CopyStats(IReadOnlyDictionary<StatType, int> source, Dictionary<StatType, int> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        foreach (var entry in source)
        {
            if (entry.Value < 0)
            {
                throw new InvalidOperationException($"Character stat '{entry.Key}' has invalid value '{entry.Value}'.");
            }

            if (entry.Value > 0)
            {
                target[entry.Key] = entry.Value;
            }
        }
    }

    private static void EnsureSingleEquippedInternalSkill(int equippedCount)
    {
        if (equippedCount > 1)
        {
            throw new InvalidOperationException("A character can equip only one internal skill.");
        }
    }
}
