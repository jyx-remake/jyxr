using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class PartyService
{
    private readonly GameSession _session;

    public PartyService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public void MoveMember(string characterId, int targetIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.MoveMember(characterId, targetIndex))
        {
            return;
        }

        _session.Events.Publish(new PartyChangedEvent());
    }

    public void Join(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (State.Party.ContainsMember(characterId))
        {
            return;
        }

        if (State.Party.MoveToMembers(characterId))
        {
            _session.Events.Publish(new PartyChangedEvent());
            return;
        }

        State.Party.AddMember(CreateInitialCharacter(characterId));
        _session.Events.Publish(new PartyChangedEvent());
    }

    public void Follow(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (State.Party.ContainsFollower(characterId))
        {
            return;
        }

        if (State.Party.MoveToFollowers(characterId))
        {
            _session.Events.Publish(new PartyChangedEvent());
            return;
        }

        State.Party.AddFollower(CreateInitialCharacter(characterId));
    }

    public void Leave(string characterIdOrName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterIdOrName);

        if (!TryFindPartyMember(characterIdOrName, out var character))
        {
            return;
        }

        State.Party.MoveToReserves(character.Id);
        _session.Events.Publish(new PartyChangedEvent());
    }

    public void LeaveFollow(string characterIdOrName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterIdOrName);

        if (!TryFindFollower(characterIdOrName, out var character))
        {
            return;
        }

        State.Party.MoveToReserves(character.Id);
    }

    public void LeaveAll()
    {
        var members = State.Party.Members;
        if (members.Count == 0)
        {
            return;
        }

        foreach (var member in members)
        {
            State.Party.MoveToReserves(member.Id);
        }

        _session.Events.Publish(new PartyChangedEvent());
    }

    public CharacterInstance RenameOrCreateReserve(string characterId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var created = false;
        if (!State.Party.TryGetCharacter(characterId, out var character) || character is null)
        {
            character = CreateInitialCharacter(characterId);
            State.Party.AddReserve(character);
            created = true;
        }

        character.Name = name;
        if (created)
        {
            _session.Events.Publish(new PartyChangedEvent());
        }

        _session.Events.Publish(new CharacterChangedEvent(character.Id));
        return character;
    }

    public IEnumerable<CharacterInstance> EnumerateActiveMembers() => State.Party.GetActiveMembers();

    public IEnumerable<CharacterInstance> EnumerateAllMembers() => State.Party.GetAllCharacters();

    public bool ContainsActiveMemberId(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        return State.Party.ContainsMember(characterId) || State.Party.ContainsFollower(characterId);
    }

    public bool ContainsActiveMemberName(string characterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        return EnumerateActiveMembers().Any(character =>
            string.Equals(character.Name, characterName, StringComparison.Ordinal) ||
            string.Equals(character.Definition.Name, characterName, StringComparison.Ordinal));
    }

    public bool TryFindActiveMember(string idOrName, out CharacterInstance character)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOrName);

        return TryFindIn(EnumerateActiveMembers(), idOrName, out character);
    }

    public bool TryFindRosterMember(string idOrName, out CharacterInstance character)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOrName);

        return TryFindIn(State.Party.GetAllCharacters(), idOrName, out character);
    }

    public bool TryFindAllMember(string id, out CharacterInstance character)
    {
        if (State.Party.TryGetCharacter(id, out var found))
        {
            character = found;
            return true;
        }

        character = null!;
        return false;
    }

    private CharacterInstance CreateInitialCharacter(string characterId)
    {
        var definition = ContentRepository.GetCharacter(characterId);
        var character = CharacterMapper.CreateInitial(characterId, definition, State.EquipmentInstanceFactory);
        character.LevelUpAllSkillsMaxLevel();
        return character;
    }

    private bool TryFindPartyMember(string idOrName, out CharacterInstance character) =>
        TryFindIn(State.Party.Members, idOrName, out character);

    private bool TryFindFollower(string idOrName, out CharacterInstance character) =>
        TryFindIn(State.Party.Followers, idOrName, out character);

    private static bool TryFindIn(
        IEnumerable<CharacterInstance> candidates,
        string idOrName,
        out CharacterInstance character)
    {
        foreach (var candidate in candidates)
        {
            if (Matches(candidate, idOrName))
            {
                character = candidate;
                return true;
            }
        }

        character = null!;
        return false;
    }

    private static bool Matches(CharacterInstance character, string idOrName) =>
        string.Equals(character.Id, idOrName, StringComparison.Ordinal) ||
        string.Equals(character.Name, idOrName, StringComparison.Ordinal) ||
        string.Equals(character.Definition.Name, idOrName, StringComparison.Ordinal);
}
