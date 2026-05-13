using System.Text.RegularExpressions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

internal sealed class StoryTextInterpolator
{
    private const string HeroVariableName = "MALE";
    private const string FemaleVariableName = "FEMALE";
    private const string ZhenlongLevelVariableName = "ZHENLONG_LEVEL";
    private static readonly Regex PlaceholderPattern = new(@"\$([A-Z_][A-Z0-9_]*)\$", RegexOptions.Compiled);

    private readonly GameSession _session;

    public StoryTextInterpolator(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

    public string Interpolate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return text;
        }

        return PlaceholderPattern.Replace(text, ReplacePlaceholder);
    }

    private string ReplacePlaceholder(Match match)
    {
        var variableName = match.Groups[1].Value;
        return TryResolvePlaceholder(variableName, out var value)
            ? value
            : match.Value;
    }

    private bool TryResolvePlaceholder(string variableName, out string value)
    {
        switch (variableName)
        {
            case HeroVariableName:
                return TryResolveCharacterName(Party.HeroCharacterId, out value);
            case FemaleVariableName:
                return TryResolveCharacterName(Party.HeroineCharacterId, out value);
            case ZhenlongLevelVariableName:
                value = (_session.Profile.ZhenlongqijuLevel + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    private bool TryResolveCharacterName(string characterId, out string value)
    {
        if (State.Party.TryGetCharacter(characterId, out var character) && character is not null)
        {
            value = character.Name;
            return true;
        }

        if (TryGetCharacterDefinitionName(characterId, out value))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    private bool TryGetCharacterDefinitionName(string characterId, out string value)
    {
        if (_session.ContentRepository.TryGetCharacter(characterId, out var character))
        {
            value = character.Name;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
