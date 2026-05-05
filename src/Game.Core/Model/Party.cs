using System.Diagnostics.CodeAnalysis;
using Game.Core.Persistence;
using Game.Core.Model.Character;

namespace Game.Core.Model;

public sealed class Party
{
    public const string HeroCharacterId = "主角";
    public const string HeroineCharacterId = "女主";

    private readonly OrderedDictionary<string, CharacterInstance> _members = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CharacterInstance> _followers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CharacterInstance> _reserves = new(StringComparer.Ordinal);

    public IReadOnlyList<CharacterInstance> Members => [.. _members.Values];

    public IReadOnlyList<CharacterInstance> Followers => [.. _followers.Values];

    public IReadOnlyList<CharacterInstance> Reserves => [.. _reserves.Values];

    public void AddMember(CharacterInstance character) => AddCharacter(character, PartyPlacement.Member);

    public void AddFollower(CharacterInstance character) => AddCharacter(character, PartyPlacement.Follower);

    public void AddReserve(CharacterInstance character) => AddCharacter(character, PartyPlacement.Reserve);

    public bool ContainsMember(string characterId) => _members.ContainsKey(characterId);

    public bool ContainsFollower(string characterId) => _followers.ContainsKey(characterId);

    public bool ContainsReserve(string characterId) => _reserves.ContainsKey(characterId);

    public bool ContainsCharacter(string characterId) => TryGetCharacter(characterId, out _);

    public bool TryGetMember(string characterId, [NotNullWhen(true)] out CharacterInstance? character) =>
        _members.TryGetValue(characterId, out character);

    public bool TryGetFollower(string characterId, [NotNullWhen(true)] out CharacterInstance? character) =>
        _followers.TryGetValue(characterId, out character);

    public bool TryGetReserve(string characterId, [NotNullWhen(true)] out CharacterInstance? character) =>
        _reserves.TryGetValue(characterId, out character);

    public bool TryGetCharacter(string characterId, [NotNullWhen(true)]  out CharacterInstance? character) =>
        TryGetPlacement(characterId, out character, out _);

    public CharacterInstance GetMember(string characterId) => _members[characterId];

    public CharacterInstance GetNextMember(string characterId) => GetRelativeMember(characterId, 1);

    public CharacterInstance GetPreviousMember(string characterId) => GetRelativeMember(characterId, -1);

    public bool MoveMember(string characterId, int targetIndex)
    {
        if (!_members.TryGetValue(characterId, out var character))
        {
            throw new InvalidOperationException($"Character '{characterId}' is not in the party.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(targetIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(targetIndex, _members.Count);

        if (string.Equals(characterId, HeroCharacterId, StringComparison.Ordinal))
        {
            return targetIndex == 0 ? false : throw new InvalidOperationException("Hero must remain at the first party position.");
        }

        if (_members.Count > 0 && string.Equals(_members.GetAt(0).Value.Id, HeroCharacterId, StringComparison.Ordinal) && targetIndex == 0)
        {
            throw new InvalidOperationException("Hero occupies the first party position and cannot be displaced.");
        }

        var currentIndex = _members.IndexOf(characterId);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException($"Character '{characterId}' is not in the party.");
        }

        if (currentIndex == targetIndex)
        {
            return false;
        }

        _members.Remove(characterId);
        _members.Insert(targetIndex, characterId, character);
        return true;
    }

    private CharacterInstance GetRelativeMember(string characterId, int offset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        if (_members.Count == 0)
        {
            throw new InvalidOperationException("Party is empty.");
        }

        if (!_members.ContainsKey(characterId))
        {
            throw new InvalidOperationException($"Character '{characterId}' is not in the party.");
        }

        var currentIndex = _members.IndexOf(characterId);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException($"Character '{characterId}' is not in the party.");
        }

        var targetIndex = (currentIndex + offset % _members.Count + _members.Count) % _members.Count;
        return _members.GetAt(targetIndex).Value;
    }

    public bool MoveToMembers(string characterId) => MoveExisting(characterId, PartyPlacement.Member);

    public bool MoveToFollowers(string characterId) => MoveExisting(characterId, PartyPlacement.Follower);

    public bool MoveToReserves(string characterId) => MoveExisting(characterId, PartyPlacement.Reserve);

    public IReadOnlyList<CharacterInstance> GetActiveMembers() => [.. Members, .. Followers];

    public IReadOnlyList<CharacterInstance> GetAllCharacters() => [.. Members, .. Followers, .. Reserves];

    public PartyRecord ToRecord() => new(
        Members.Select(member => member.Id).ToList(),
        Followers.Select(member => member.Id).ToList(),
        Reserves.Select(member => member.Id).ToList());

    public static Party FromRecord(PartyRecord record, IReadOnlyDictionary<string, CharacterInstance> characters)
    {
        var party = new Party();
        foreach (var memberId in record.MemberIds)
        {
            if (!characters.TryGetValue(memberId, out var character))
            {
                throw new InvalidOperationException($"Party references missing character '{memberId}'.");
            }

            party.AddMember(character);
        }

        foreach (var memberId in record.FollowerIds)
        {
            if (!characters.TryGetValue(memberId, out var character))
            {
                throw new InvalidOperationException($"Party followers reference missing character '{memberId}'.");
            }

            party.AddFollower(character);
        }

        foreach (var memberId in record.ReserveIds)
        {
            if (!characters.TryGetValue(memberId, out var character))
            {
                throw new InvalidOperationException($"Party reserves reference missing character '{memberId}'.");
            }

            party.AddReserve(character);
        }

        return party;
    }

    private void AddCharacter(CharacterInstance character, PartyPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(character);

        if (ContainsCharacter(character.Id))
        {
            throw new InvalidOperationException($"Character '{character.Id}' is already in the party roster.");
        }

        AddToPool(character, placement);
    }

    private bool MoveExisting(string characterId, PartyPlacement targetPlacement)
    {
        if (!TryGetPlacement(characterId, out var character, out var sourcePlacement))
        {
            return false;
        }

        if (sourcePlacement == targetPlacement)
        {
            return false;
        }

        GetPool(sourcePlacement).Remove(characterId);
        AddToPool(character, targetPlacement);
        return true;
    }

    private bool TryGetPlacement(
        string characterId,
        out CharacterInstance character,
        out PartyPlacement placement)
    {
        foreach (var placementCandidate in Enum.GetValues<PartyPlacement>())
        {
            if (GetPool(placementCandidate).TryGetValue(characterId, out var found))
            {
                character = found;
                placement = placementCandidate;
                return true;
            }
        }

        character = null!;
        placement = default;
        return false;
    }

    private void AddToPool(CharacterInstance character, PartyPlacement placement)
    {
        var added = GetPool(placement).TryAdd(character.Id, character);

        if (!added)
        {
            throw new InvalidOperationException($"Character '{character.Id}' is already in party placement '{placement}'.");
        }
    }

    private IDictionary<string, CharacterInstance> GetPool(PartyPlacement placement) =>
        placement switch
        {
            PartyPlacement.Member => _members,
            PartyPlacement.Follower => _followers,
            PartyPlacement.Reserve => _reserves,
            _ => throw new InvalidOperationException($"Unsupported party placement '{placement}'."),
        };

    private enum PartyPlacement
    {
        Member,
        Follower,
        Reserve,
    }

}
