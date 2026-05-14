using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class NewGameStateFactory
{
    private readonly IContentRepository _contentRepository;
    private readonly GameConfig _config;

    public NewGameStateFactory(IContentRepository contentRepository, GameConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        _contentRepository = contentRepository;
        _config = config ?? new GameConfig();
    }

    public GameState Create(
        IReadOnlyList<string> initialPartyCharacterIds,
        int round = 1,
        int gold = 0,
        ChestState? chest = null)
    {
        ArgumentNullException.ThrowIfNull(initialPartyCharacterIds);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(gold);
        if (initialPartyCharacterIds.Count == 0)
        {
            throw new InvalidOperationException("Session flow requires at least one initial party character.");
        }

        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var party = new Party();
        foreach (var characterId in initialPartyCharacterIds)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new InvalidOperationException("Initial party character id cannot be empty.");
            }

            var definition = _contentRepository.GetCharacter(characterId);
            var member = CharacterMapper.CreateInitial(characterId, definition, equipmentInstanceFactory, _config);
            member.LevelUpAllSkillsMaxLevel();
            party.AddMember(member);
        }

        var adventure = new AdventureState();
        adventure.SetRound(round);

        var currency = new CurrencyState();
        currency.AddGold(gold);

        var state = new GameState();
        state.SetAdventure(adventure);
        state.SetParty(party);
        state.SetChest(chest ?? new ChestState());
        state.SetEquipmentInstanceFactory(equipmentInstanceFactory);
        state.SetCurrency(currency);
        return state;
    }
}
