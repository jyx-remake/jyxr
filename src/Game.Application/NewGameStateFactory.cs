using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Application;

public sealed class NewGameStateFactory
{
    private readonly IContentRepository _contentRepository;
    private readonly GameConfig _config;
    private readonly SkillMaxLevelPolicy? _skillMaxLevelPolicy;
    private readonly Func<GameProfile> _profileProvider;

    public NewGameStateFactory(
        IContentRepository contentRepository,
        GameConfig? config = null,
        SkillMaxLevelPolicy? skillMaxLevelPolicy = null,
        Func<GameProfile>? profileProvider = null)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        _contentRepository = contentRepository;
        _config = config ?? new GameConfig();
        _skillMaxLevelPolicy = skillMaxLevelPolicy;
        _profileProvider = profileProvider ?? (() => new GameProfile());
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
        var initialCharacterFactory = CreateInitialCharacterFactory(round);
        foreach (var characterId in initialPartyCharacterIds)
        {
            party.AddMember(initialCharacterFactory.Create(characterId, equipmentInstanceFactory));
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

    private InitialCharacterFactory CreateInitialCharacterFactory(int round)
    {
        var skillMaxLevelPolicy = _skillMaxLevelPolicy ??
            new SkillMaxLevelPolicy(_config, _profileProvider(), round);
        return new InitialCharacterFactory(_contentRepository, _config, skillMaxLevelPolicy);
    }
}
