namespace Game.Core.Model;

public sealed class GameState
{
    public AdventureState Adventure { get; private set; } = new();

    public Party Party { get; private set; } = new();

    public Inventory Inventory { get; private set; } = new();

    public ChestState Chest { get; private set; } = new();

    public EquipmentInstanceFactory EquipmentInstanceFactory { get; private set; } = new();

    public CurrencyState Currency { get; private set; } = new();

    public ClockState Clock { get; private set; } = new();

    public LocationState Location { get; private set; } = new();

    public MapEventProgressState MapEventProgress { get; private set; } = new();

    public ShopState Shop { get; private set; } = new();

    public StoryState Story { get; private set; } = new();

    public JournalState Journal { get; private set; } = new();

    public void SetAdventure(AdventureState adventure) => Adventure = adventure;

    public void SetParty(Party party) => Party = party;

    public void SetInventory(Inventory inventory) => Inventory = inventory;

    public void SetChest(ChestState chest) => Chest = chest;

    public void SetEquipmentInstanceFactory(EquipmentInstanceFactory equipmentInstanceFactory) =>
        EquipmentInstanceFactory = equipmentInstanceFactory;

    public void SetCurrency(CurrencyState currency) => Currency = currency;

    public void SetClock(ClockState clock) => Clock = clock;

    public void SetLocation(LocationState location) => Location = location;

    public void SetMapEventProgress(MapEventProgressState mapEventProgress) => MapEventProgress = mapEventProgress;

    public void SetShop(ShopState shop) => Shop = shop;

    public void SetStory(StoryState story) => Story = story;

    public void SetJournal(JournalState journal) => Journal = journal;
}
