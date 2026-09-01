using System.Text.RegularExpressions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class StoryTextInterpolator
{
    private const string HeroVariableName = "MALE";
    private const string FemaleVariableName = "FEMALE";
    private const string ZhenlongLevelVariableName = "ZHENLONG_LEVEL";
    // Keep this in step with ExpressionSymbol: built-ins use upper-case Latin,
    // while native DSL variables may use lower-case Latin or Han identifiers.
    // Built-in placeholders are resolved first and unknown names stay verbatim.
    private static readonly Regex PlaceholderPattern = new(
        @"\$([A-Za-z_\u3007\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF][A-Za-z0-9_\u3007\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]*)\$",
        RegexOptions.Compiled);

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
                return TryResolveStoryVariable(variableName, out value);
        }
    }

    private bool TryResolveStoryVariable(string variableName, out string value)
    {
        if (_session.State.Story.TryGetVariable(variableName, out var variable))
        {
            value = variable.ToString();
            return true;
        }

        value = string.Empty;
        return false;
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
