namespace Game.Core.Definitions;

public sealed record ResourceDefinition
{
    public required string Id { get; init; }

    public string? Group { get; init; }

    public string Value { get; init; } = "";
}

public sealed record GameTipDefinition
{
    public required string Id { get; init; }

    public required string Text { get; init; }
}
