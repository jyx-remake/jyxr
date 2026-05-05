using Game.Core.Model.Character;
using Game.Core.Model;

namespace Game.Godot;

public static class PartyAccess
{
	public const string HeroCharacterId = Party.HeroCharacterId;

	public static CharacterInstance Hero => GetRequired(HeroCharacterId);

	public static CharacterInstance GetRequired(string characterId)
	{
		var party = Game.State.Party;
		if (!party.TryGetMember(characterId, out var character) || character is null)
		{
			throw new InvalidOperationException($"Current party does not contain character '{characterId}'.");
		}

		return character;
	}
}
