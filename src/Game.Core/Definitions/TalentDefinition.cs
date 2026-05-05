using System.Text.Json.Serialization;
using Game.Core.Affix;

namespace Game.Core.Definitions;

public sealed record TalentDefinition: IAffixProvider
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Point { get; init; } = 0;
    public string Description { get; init; } = "";
    public IReadOnlyList<string> ReplaceTalentIds { get; init; } = [];
    [JsonIgnore]
    public ProviderKind ProviderKind { get; } = ProviderKind.Talent;
    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];
}
