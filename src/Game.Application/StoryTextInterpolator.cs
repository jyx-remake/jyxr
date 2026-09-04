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

    // Legacy authoring wraps gender aliases in doubled dollars ($$性别1$$);
    // they must be resolved before the single-dollar pass, otherwise the
    // single-dollar pattern would match the inner $性别1$ and leave the outer
    // dollars around the resolved text.
    private static readonly Regex DoubledGenderAliasPattern = new(
        @"\$\$(性别[123])\$\$",
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

        text = DoubledGenderAliasPattern.Replace(text, match => ResolveGenderAlias(match.Groups[1].Value));
        return PlaceholderPattern.Replace(text, ReplacePlaceholder);
    }

    private string ResolveGenderAlias(string variableName) =>
        StoryGenderAlias.TryResolve(variableName, ResolveHeroGender(), out var value)
            ? value
            : string.Empty;

    private CharacterGender ResolveHeroGender() =>
        State.Party.TryGetCharacter(Party.HeroCharacterId, out var hero) && hero is not null
            ? hero.Gender
            : CharacterGender.Male;

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
            case "性别1":
            case "性别2":
            case "性别3":
                return StoryGenderAlias.TryResolve(variableName, ResolveHeroGender(), out value);
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
