using System.Text.Json.Serialization;
using Game.Core.Abstractions;

namespace Game.Core.Definitions;

[method: JsonConstructor]
public sealed record InitialCharacterTitleEntryDefinition(
    string Id,
    bool Equipped = false)
{
    [JsonIgnore]
    public CharacterTitleDefinition Title { get; private set; } = null!;

    public InitialCharacterTitleEntryDefinition(
        CharacterTitleDefinition title,
        bool Equipped = false)
        : this(title.Id, Equipped)
    {
        Title = title;
    }

    public void Resolve(IContentRepository contentRepository)
    {
        Title = contentRepository.GetCharacterTitle(Id);
    }
}
