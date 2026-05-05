namespace Game.Core.Definitions;

public sealed record SectDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string StoryId { get; init; } = "";

    public string PrimaryFocus { get; init; } = "";

    public string Description { get; init; } = "";

    public string? Portrait { get; init; }

    public IReadOnlyList<string> SignatureSkillNames { get; init; } = [];

    public IReadOnlyList<string> MasterNames { get; init; } = [];

    public string? Background { get; init; }

    public IReadOnlyList<string> TraitTags { get; init; } = [];
}
