using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public static class GameExpressionSymbols
{
    public static IReadOnlySet<string> BuiltInVariables { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "silver", "yuanbao", "round", "difficulty", "sect", "morality", "daode", "rank", "elapsed_days",
        "current_map", "current_time_slot", "friend_count",
    };

    public static void ValidateDynamicVariables(GameState state, StoryExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);
        foreach (var name in state.Story.Variables.Keys)
        {
            if (BuiltInVariables.Contains(name) || context.Variables.ContainsKey(name))
            {
                throw new InvalidOperationException($"Story variable '{name}' conflicts with a reserved expression symbol.");
            }
        }

        foreach (var name in context.Variables.Keys)
        {
            if (BuiltInVariables.Contains(name))
            {
                throw new InvalidOperationException($"Execution-context variable '{name}' conflicts with a built-in expression symbol.");
            }
        }
    }
}

internal sealed class GameExpressionVariableResolver : IExpressionVariableResolver
{
    private readonly GameSession _session;
    private readonly StoryExecutionContext _context;

    public GameExpressionVariableResolver(GameSession session, StoryExecutionContext context)
    {
        _session = session;
        _context = context;
    }

    public bool TryResolve(string name, out ExpressionValue value)
    {
        var state = _session.State;
        value = name switch
        {
            "silver" => ExpressionValue.FromNumber(state.Currency.Silver),
            "yuanbao" => ExpressionValue.FromNumber(_session.Profile.Yuanbao),
            "round" => ExpressionValue.FromNumber(state.Adventure.Round),
            "difficulty" => ExpressionValue.FromString(state.Adventure.GetModeId()),
            "sect" => ExpressionValue.FromString(state.Adventure.SectId ?? string.Empty),
            "morality" => ExpressionValue.FromNumber(state.Adventure.Morality),
            "daode" => ExpressionValue.FromNumber(state.Adventure.Morality),
            "rank" => ExpressionValue.FromNumber(state.Adventure.Rank),
            "elapsed_days" => ExpressionValue.FromNumber(state.Clock.TotalDays),
            "current_map" => ExpressionValue.FromString(state.Location.CurrentMapId ?? string.Empty),
            "current_time_slot" => ExpressionValue.FromString(state.Clock.TimeSlot.ToChineseBranch()),
            "friend_count" => ExpressionValue.FromNumber(state.Party.Members.Count),
            _ => default,
        };

        return GameExpressionSymbols.BuiltInVariables.Contains(name)
            || _context.Variables.TryGetValue(name, out value)
            || state.Story.TryGetVariable(name, out value);
    }
}

public sealed class GameExpressionEnvironment
{
    private readonly GameSession _session;
    private readonly ExpressionFunctionRegistry _functions;

    public GameExpressionEnvironment(GameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _functions = new ExpressionFunctionRegistryBuilder()
            .AddLibrary(new CoreExpressionFunctions())
            .AddLibrary(new InventoryQueryFunctions(session))
            .AddLibrary(new AdventureQueryFunctions(session))
            .AddLibrary(new MapQueryFunctions(session))
            .AddLibrary(new StoryQueryFunctions(session))
            .AddLibrary(new PartyCharacterQueryFunctions(session))
            .AddLibrary(new RandomQueryFunctions(session))
            .Build();
    }

    public ExpressionEnvironment Create(StoryExecutionContext? context = null)
    {
        context ??= StoryExecutionContext.Empty;
        GameExpressionSymbols.ValidateDynamicVariables(_session.State, context);
        return new ExpressionEnvironment(new GameExpressionVariableResolver(_session, context), _functions);
    }
}

internal sealed class InventoryQueryFunctions
{
    private readonly GameSession _session;
    public InventoryQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("item_count")]
    public int ItemCount(string id)
    {
        if (!_session.ContentRepository.TryGetItem(id, out var item))
        {
            _session.DiagnosticLogger.Warning($"Function 'item_count' treated unknown item '{id}' as zero.");
            return 0;
        }

        return _session.State.Inventory.Entries
            .OfType<StackInventoryEntry>()
            .Where(entry => string.Equals(entry.Definition.Id, item.Id, StringComparison.Ordinal))
            .Sum(entry => entry.Quantity);
    }
}

internal sealed class AdventureQueryFunctions
{
    private readonly GameSession _session;
    public AdventureQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("favorability", "haogan")]
    public int Favorability(string characterId = AdventureState.DefaultFavorabilityTargetId) =>
        _session.State.Adventure.GetFavorability(characterId);
}

internal sealed class MapQueryFunctions
{
    private readonly GameSession _session;
    public MapQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("map_event_completed")]
    public bool MapEventCompleted(string mapId, string locationId, string eventId) =>
        _session.State.MapEventProgress.IsCompleted(mapId, locationId, eventId);
}

internal sealed class StoryQueryFunctions
{
    private readonly GameSession _session;
    public StoryQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("story_completed", "should_finish")]
    public bool StoryCompleted(string id) => _session.State.Story.IsStoryCompleted(id);

    [ExpressionFunction("last_story_is", "follow_story")]
    public bool LastStoryIs(string id) =>
        string.Equals(_session.State.Story.LastStoryId, id, StringComparison.Ordinal);

    [ExpressionFunction("has_time_key")]
    public bool HasTimeKey(string key) => _session.State.Story.HasTimeKey(key);

    [ExpressionFunction("zhoumu_greater_than")]
    public bool RoundGreaterThan(int round) => _session.State.Adventure.Round > round;

    [ExpressionFunction("has_var")]
    public bool HasVariable(string name) => _session.State.Story.TryGetVariable(name, out _);

    [ExpressionFunction("has_flag")]
    public bool HasFlag(string name)
    {
        if (!_session.State.Story.TryGetVariable(name, out var value))
            return false;

        return value.AsBoolean($"Story flag '{name}'");
    }

    [ExpressionFunction("has_achievement", "have_nick")]
    public bool HasAchievement(string id) => _session.Profile.IsAchievementUnlocked(id);

    [ExpressionFunction("is_zhujue_head")]
    public bool IsMainCharacterPortrait(string portraitId)
    {
        if (!_session.State.Party.TryGetCharacter(Party.HeroCharacterId, out var hero) || hero is null)
        {
            return false;
        }

        return string.Equals(hero.Portrait, portraitId, StringComparison.Ordinal);
    }

    [ExpressionFunction("is_zhujue_name")]
    public bool IsMainCharacterName(string name)
    {
        return _session.State.Party.TryGetCharacter(Party.HeroCharacterId, out var hero) &&
            hero is not null &&
            string.Equals(hero.Name, name, StringComparison.Ordinal);
    }

    [ExpressionFunction("has_talent")]
    public bool HasTalent(string characterId, string talentId) =>
        GetActiveCharacter(characterId).UnlockedTalents.Any(talent =>
            string.Equals(talent.Id, talentId, StringComparison.Ordinal));

    [ExpressionFunction("has_title")]
    public bool HasTitle(string characterId, string titleId) =>
        GetActiveCharacter(characterId).Titles.Any(title =>
            string.Equals(title.Id, titleId, StringComparison.Ordinal));

    [ExpressionFunction("has_skill")]
    public bool HasSkill(string characterId, string skillId)
    {
        var character = GetActiveCharacter(characterId);
        return character.ExternalSkills.Any(skill => string.Equals(skill.Definition.Id, skillId, StringComparison.Ordinal))
            || character.InternalSkills.Any(skill => string.Equals(skill.Definition.Id, skillId, StringComparison.Ordinal))
            || character.SpecialSkills.Any(skill => string.Equals(skill.Definition.Id, skillId, StringComparison.Ordinal));
    }

    private CharacterInstance GetActiveCharacter(string characterId) =>
        _session.State.Party.GetActiveMembers()
            .FirstOrDefault(character => string.Equals(character.Id, characterId, StringComparison.Ordinal)
                || string.Equals(character.Name, characterId, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Character '{characterId}' is not in the active party.");
}

internal sealed class PartyCharacterQueryFunctions
{
    private readonly GameSession _session;
    public PartyCharacterQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("in_team", "active_party_contains")]
    public bool ActivePartyContains(string characterId) =>
        _session.PartyService.ContainsActiveMemberId(characterId);

    [ExpressionFunction("character_level")]
    public int CharacterLevel(string characterId) => GetActiveCharacter(characterId).Level;

    [ExpressionFunction("character_stat")]
    public int CharacterStat(string characterId, string stat) =>
        GetActiveCharacter(characterId).GetBaseStat(StatCatalog.Parse(stat));

    [ExpressionFunction("skill_level")]
    public int SkillLevel(string characterId, string skillId)
    {
        var character = GetActiveCharacter(characterId);
        return character.GetExternalSkillLevel(skillId) ?? character.GetInternalSkillLevel(skillId) ?? 0;
    }

    private CharacterInstance GetActiveCharacter(string characterId) =>
        _session.State.Party.GetActiveMembers()
            .FirstOrDefault(character => string.Equals(character.Id, characterId, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Character '{characterId}' is not in the active party.");
}

internal sealed class RandomQueryFunctions
{
    private readonly GameSession _session;
    public RandomQueryFunctions(GameSession session) => _session = session;

    [ExpressionFunction("chance")]
    public bool Chance(double probability)
    {
        if (probability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probability),
                "chance probability must be between 0 and 1.");
        }

        return _session.RandomService.NextDouble() < probability;
    }
}
