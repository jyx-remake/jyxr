using System.Text.Json.Serialization;
using Game.Core.Affix;

namespace Game.Core.Definitions;

public sealed record CharacterTitleDefinition : IAffixProvider
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Icon { get; init; }
    public string Description { get; init; } = "";
    public double Attack { get; init; }
    public double Defence { get; init; }
    public double Hard { get; init; }
    public double AoyiProbabilityAdd { get; init; }
    public double AoyiPowerAdd { get; init; }
    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];
    [JsonIgnore]
    public ProviderKind ProviderKind => ProviderKind.CharacterTitle;
}
