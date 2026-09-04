using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class PartyService
{
    private readonly GameSession _session;
    private readonly InitialCharacterFactory _initialCharacterFactory;

    public PartyService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _initialCharacterFactory = new InitialCharacterFactory(
            session.ContentRepository,
            session.Config,
            session.SkillMaxLevelPolicy);
    }

    private GameState State => _session.State;

    public void MoveMember(string characterId, int targetIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.MoveMember(characterId, targetIndex))
        {
            return;
        }

        _session.Events.Publish(new PartyChangedEvent());
    }

    public void Join(string characterId, string? definitionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        var resolvedDefinitionId = definitionId ?? characterId;
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedDefinitionId);
        _session.ContentRepository.GetCharacter(resolvedDefinitionId);

            if (State.Party.TryGetCharacter(characterId, out var existing))
            {
                // Legacy parity: permanently-left and kicked characters cannot be
                // brought back through the plain join command.
                if (existing.LeaveState is CharacterLeaveState.Permanent or CharacterLeaveState.Kicked)
                {
                    _session.DiagnosticLogger.Info($"Join skipped: '{characterId}' has permanently left or was kicked.");
                    return;
                }

                if (State.Party.MoveToMembers(characterId))
                {
                    existing.ClearLeaveState();
                    _session.Events.Publish(new PartyChangedEvent());
                }

                return;
            }

        State.Party.AddMember(CreateInitialCharacter(characterId, resolvedDefinitionId));
        _session.Events.Publish(new PartyChangedEvent());
    }

    /// <summary>
    /// Recalls a temporarily-left character back into the active party
    /// (legacy TEMP_JOIN). Only characters left through
    /// <c>leave(character, 'temp')</c> can be recalled this way.
    /// </summary>
    public void JoinTemp(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.TryGetCharacter(characterId, out var character) ||
            character.LeaveState != CharacterLeaveState.Temp)
        {
            _session.DiagnosticLogger.Info($"JoinTemp skipped: '{characterId}' is not temporarily left.");
            return;
        }

        if (State.Party.MoveToMembers(characterId))
        {
            character.ClearLeaveState();
            _session.Events.Publish(new PartyChangedEvent());
        }
    }

    /// <summary>
    /// Player-initiated kick (the party UI leave button, legacy
    /// ButtonLidui = AddManualTemp + JOIN_TEMP): the character leaves the
    /// active party and can only be brought back through the recall UI.
    /// </summary>
    public void Kick(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (string.Equals(characterId, Party.HeroCharacterId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The hero cannot be kicked from the party.");
        }

        if (!State.Party.TryGetMember(characterId, out var character))
        {
            return;
        }

        MoveToReserves(character);
        character.SetLeaveState(CharacterLeaveState.Kicked);
        _session.Events.Publish(new PartyChangedEvent());
    }

    /// <summary>
    /// Recalls a kicked character back into the active party (the recall UI;
    /// legacy REJOIN_MENU/rejoin). Only kicked characters can be recalled.
    /// </summary>
    public void RecallKicked(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.TryGetCharacter(characterId, out var character) ||
            character.LeaveState != CharacterLeaveState.Kicked)
        {
            _session.DiagnosticLogger.Info($"RecallKicked skipped: '{characterId}' is not kicked.");
            return;
        }

        if (State.Party.MoveToMembers(characterId))
        {
            character.ClearLeaveState();
            _session.Events.Publish(new PartyChangedEvent());
        }
    }

    public void Follow(string characterId, string? definitionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        var resolvedDefinitionId = definitionId ?? characterId;
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedDefinitionId);
        _session.ContentRepository.GetCharacter(resolvedDefinitionId);

        if (State.Party.ContainsFollower(characterId))
        {
            return;
        }

        if (State.Party.MoveToFollowers(characterId))
        {
            _session.Events.Publish(new PartyChangedEvent());
            return;
        }

        State.Party.AddFollower(CreateInitialCharacter(characterId, resolvedDefinitionId));
    }

    /// <summary>
    /// Plain leave: moves the character to the reserves without recording a
    /// leave state (UI-driven roster changes; not blocked by the join guard).
    /// </summary>
    public void Leave(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.TryGetMember(characterId, out var character))
        {
            return;
        }

        MoveToReserves(character);
        _session.Events.Publish(new PartyChangedEvent());
    }

    /// <summary>
    /// Permanent leave (legacy LEAVE_TEMP semantics minus the equipment
    /// drop): the character leaves the active party and cannot be brought
    /// back by the plain join command.
    /// </summary>
    public void LeavePermanent(string characterId) => LeaveCore(characterId, CharacterLeaveState.Permanent);

    /// <summary>
    /// Temporary leave (legacy JOIN_TEMP semantics): the character leaves the
    /// active party but stays recallable through join temp.
    /// </summary>
    public void LeaveTemp(string characterId) => LeaveCore(characterId, CharacterLeaveState.Temp);

    private void LeaveCore(string characterId, CharacterLeaveState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.TryGetMember(characterId, out var character))
        {
            return;
        }

        MoveToReserves(character);
        character.SetLeaveState(state);
        _session.Events.Publish(new PartyChangedEvent());
    }

    public void LeaveFollow(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        if (!State.Party.TryGetFollower(characterId, out var character))
        {
            return;
        }

        MoveToReserves(character);
        _session.Events.Publish(new PartyChangedEvent());
    }

    public void LeaveAll()
    {
        var departingMembers = State.Party.Members
            .Where(member => !string.Equals(member.Id, Party.HeroCharacterId, StringComparison.Ordinal))
            .ToArray();
        if (departingMembers.Length == 0)
        {
            return;
        }

        foreach (var member in departingMembers)
        {
            MoveToReserves(member);
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
            character = CreateInitialCharacter(characterId, characterId);
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

    private CharacterInstance CreateInitialCharacter(string characterId, string definitionId)
    {
        return _initialCharacterFactory.Create(characterId, definitionId, State.EquipmentInstanceFactory);
    }

    private void MoveToReserves(CharacterInstance character)
    {
        _session.InventoryService.UnequipAllToInventory(character);
        State.Party.MoveToReserves(character.Id);
    }
}
