namespace Game.Core.Definitions;

public sealed record ItemTagDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Order { get; init; }
}
