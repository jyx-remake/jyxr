using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Core.Definitions.Skills;

public enum SkillImpactType
{
    [JsonStringEnumMemberName("single")]
    Single,
    [JsonStringEnumMemberName("plus")]
    Plus,
    [JsonStringEnumMemberName("star")]
    Star,
    [JsonStringEnumMemberName("line")]
    Line,
    [JsonStringEnumMemberName("square")]
    Square,
    [JsonStringEnumMemberName("fan")]
    Fan,
    [JsonStringEnumMemberName("ring")]
    Ring,
    [JsonStringEnumMemberName("x")]
    X,
    [JsonStringEnumMemberName("cleave")]
    Cleave,
}

public sealed record SkillCostDefinition(
    int? Mp = null,
    int Rage = 0)
{
    public static SkillCostDefinition None { get; } = new();
}


[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SkillTargetingDefinition(
    bool? CanCastAtSelf = null,
    bool? CanImpactSelf = null,
    string? CastType = null,
    int? CastSize = null,
    SkillImpactType? ImpactType = null,
    int? ImpactSize = null)
{
    public static SkillTargetingDefinition None { get; } = new();
}

public static class SkillTargetingDefaults
{
    public const bool CanImpactSelf = false;

    public static bool CanCastAtSelf(SkillImpactType impactType) => impactType switch
    {
        SkillImpactType.Plus or
        SkillImpactType.Star or
        SkillImpactType.Square or
        SkillImpactType.Ring or
        SkillImpactType.X => true,
        SkillImpactType.Single or
        SkillImpactType.Line or
        SkillImpactType.Fan or
        SkillImpactType.Cleave => false,
        _ => throw new ArgumentOutOfRangeException(nameof(impactType), impactType, null),
    };
}

[method: JsonConstructor]
public sealed record SkillBuffDefinition(
    string Id,
    int Level = 1,
    int Duration = 3,
    int? Chance = null)
{

    [JsonIgnore]
    public BuffDefinition Buff { get; private set; } = null!;

    public SkillBuffDefinition(
        BuffDefinition buff,
        int level = 1,
        int duration = 3,
        int? chance = null)
        : this(buff.Id, level, duration, chance)
    {
        Buff = buff;
    }

    public void Resolve(IContentRepository contentRepository)
    {
        Buff = contentRepository.GetBuff(Id);
    }
}
