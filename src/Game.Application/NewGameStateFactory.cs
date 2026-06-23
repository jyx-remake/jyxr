using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Application;

public sealed class NewGameStateFactory
{
    private readonly InitialCharacterFactory _initialCharacterFactory;
    private readonly GameConfig _config;

    public NewGameStateFactory(IContentRepository contentRepository, GameConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        _config = config ?? new GameConfig();
        _initialCharacterFactory = new InitialCharacterFactory(contentRepository, _config);
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
            party.AddMember(_initialCharacterFactory.Create(characterId, equipmentInstanceFactory));
        }

        var adventure = new AdventureState();
        adventure.SetRound(round);

        var currency = new CurrencyState();
        currency.AddGold(gold);

        var state = new GameState();
        state.SetAdventure(adventure);
        state.SetParty(party);
        state.Location.SetLargeMapPosition("大地图", _config.DefaultLargeMapPosition);
        state.SetChest(chest ?? new ChestState());
        state.SetEquipmentInstanceFactory(equipmentInstanceFactory);
        state.SetCurrency(currency);
        return state;
    }
}
