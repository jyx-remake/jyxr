using System.Text.Json.Serialization;

namespace Game.Core.Definitions.Skills;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RequiredExternalSkillLevelLegendConditionDefinition), "skill")]
[JsonDerivedType(typeof(RequiredInternalSkillLevelLegendConditionDefinition), "internal_skill")]
[JsonDerivedType(typeof(RequiredSpecialSkillLegendConditionDefinition), "special_skill")]
[JsonDerivedType(typeof(RequiredTalentLegendConditionDefinition), "talent")]
public abstract record LegendSkillConditionDefinition;

public sealed record RequiredExternalSkillLevelLegendConditionDefinition(
    string TargetId,
    int Level) : LegendSkillConditionDefinition;

public sealed record RequiredInternalSkillLevelLegendConditionDefinition(
    string TargetId,
    int Level) : LegendSkillConditionDefinition;

public sealed record RequiredSpecialSkillLegendConditionDefinition(
    string TargetId) : LegendSkillConditionDefinition;

public sealed record RequiredTalentLegendConditionDefinition(
    string TargetId) : LegendSkillConditionDefinition;
