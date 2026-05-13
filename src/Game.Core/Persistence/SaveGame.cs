using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Core.Persistence;

public sealed record SaveGame(
    int Version,
    AdventureStateRecord Adventure,
    IReadOnlyList<CharacterRecord> Characters,
    PartyRecord Party,
    InventoryRecord Inventory,
    ChestStateRecord? Chest,
    EquipmentInstanceFactoryRecord EquipmentInstanceFactory,
    CurrencyRecord Currency,
    ClockRecord Clock,
    LocationRecord Location,
    MapEventProgressRecord MapEventProgress,
    WorldTriggerStateRecord? WorldTriggerState = null,
    StoryStateRecord? StoryState = null,
    JournalRecord? Journal = null,
    ShopStateRecord? ShopState = null,
    SpecialBattleStateRecord? SpecialBattleState = null)
{
    public const int CurrentVersion = 20;

    public static SaveGame Create(
        AdventureState adventure,
        Party party,
        Inventory inventory,
        ChestState chest,
        EquipmentInstanceFactory equipmentInstanceFactory,
        CurrencyState currency,
        ClockState clock,
        LocationState location,
        MapEventProgressState mapEventProgress,
        WorldTriggerState worldTriggerState,
        StoryState? storyState = null,
        JournalState? journal = null,
        ShopState? shopState = null,
        SpecialBattleState? specialBattleState = null)
    {
        ArgumentNullException.ThrowIfNull(adventure);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(chest);
        ArgumentNullException.ThrowIfNull(equipmentInstanceFactory);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(mapEventProgress);
        ArgumentNullException.ThrowIfNull(worldTriggerState);

        return new SaveGame(
            CurrentVersion,
            adventure.ToRecord(),
            party.GetAllCharacters().Select(CharacterMapper.ToRecord).ToList(),
            party.ToRecord(),
            InventoryMapper.ToRecord(inventory),
            chest.ToRecord(),
            equipmentInstanceFactory.ToRecord(),
            currency.ToRecord(),
            clock.ToRecord(),
            location.ToRecord(),
            mapEventProgress.ToRecord(),
            worldTriggerState.ToRecord(),
            (storyState ?? new StoryState()).ToRecord(),
            (journal ?? new JournalState()).ToRecord(),
            (shopState ?? new ShopState()).ToRecord(),
            (specialBattleState ?? new SpecialBattleState()).ToRecord());
    }

    public AdventureState RestoreAdventureState() =>
        Game.Core.Model.AdventureState.Restore(Adventure);

    public IReadOnlyDictionary<string, CharacterInstance> RestoreCharacters(IContentRepository contentRepository)
    {
        return Characters
            .Select(record => CharacterMapper.FromRecord(record, contentRepository))
            .ToDictionary(character => character.Id, StringComparer.Ordinal);
    }

    public Party RestoreParty(IReadOnlyDictionary<string, CharacterInstance> characters) =>
        Game.Core.Model.Party.FromRecord(Party, characters);

    public Inventory RestoreInventory(IContentRepository contentRepository) =>
        InventoryMapper.FromRecord(Inventory, contentRepository);

    public ChestState RestoreChestState(IContentRepository contentRepository) =>
        ChestState.Restore(Chest, contentRepository);

    public EquipmentInstanceFactory RestoreEquipmentInstanceFactory() =>
        Game.Core.Model.EquipmentInstanceFactory.Restore(EquipmentInstanceFactory.NextNumber);

    public CurrencyState RestoreCurrency() =>
        CurrencyState.Restore(Currency);

    public ClockState RestoreClock() =>
        ClockState.Restore(Clock);

    public LocationState RestoreLocation() =>
        LocationState.Restore(Location);

    public MapEventProgressState RestoreMapEventProgress() =>
        MapEventProgressState.Restore(MapEventProgress);

    public WorldTriggerState RestoreWorldTriggerState() =>
        Game.Core.Model.WorldTriggerState.Restore(WorldTriggerState);

    public ShopState RestoreShopState() =>
        Game.Core.Model.ShopState.Restore(ShopState);

    public StoryState RestoreStoryState() =>
        Game.Core.Model.StoryState.Restore(StoryState);

    public JournalState RestoreJournal() =>
        JournalState.Restore(Journal);

    public SpecialBattleState RestoreSpecialBattleState() =>
        Game.Core.Model.SpecialBattleState.Restore(SpecialBattleState);
}
