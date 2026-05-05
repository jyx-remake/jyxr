using Game.Core.Abstractions;
using Game.Core.Model.Character;

namespace Game.Application.Formatters;

public static class CharacterBiographyFormatter
{
    public static string FormatCn(CharacterInstance character, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(contentRepository);

        foreach (var key in GetCandidateKeys(character))
        {
            if (contentRepository.TryGetResource(key, out var resource) &&
                !string.IsNullOrWhiteSpace(resource.Value))
            {
                return resource.Value.Trim();
            }
        }

        return "暂无人物传记。";
    }

    private static IEnumerable<string> GetCandidateKeys(CharacterInstance character)
    {
        yield return $"人物.{character.Name}";
        yield return $"人物.{character.Definition.Name}";
        yield return $"人物.{character.Id}";
        yield return $"人物.{character.Definition.Id}";
    }
}
