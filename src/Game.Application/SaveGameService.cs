using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Persistence;

namespace Game.Application;

public sealed class SaveGameService
{
	private readonly GameSession _session;
	private readonly IDiagnosticLogger _logger;

	public SaveGameService(GameSession session, IDiagnosticLogger? logger = null)
	{
		_session = session;
		_logger = logger ?? NullDiagnosticLogger.Instance;
	}

	private GameState State => _session.State;
	private IContentRepository ContentRepository => _session.ContentRepository;

	public SaveGame CreateSave()
	{
		var saveGame = SaveGame.Create(
			State.Adventure,
			State.Party,
			State.Inventory,
			State.Chest,
			State.EquipmentInstanceFactory,
			State.Currency,
			State.Clock,
			State.Location,
			State.MapEventProgress,
			State.Story,
			State.Journal,
			State.Shop);
		_logger.Info($"Created save game with {saveGame.Characters.Count} character(s).");
		return saveGame;
	}

	public void LoadSave(SaveGame saveGame)
	{
		var adventure = saveGame.RestoreAdventureState();
		var characters = saveGame.RestoreCharacters(ContentRepository);
		var party = saveGame.RestoreParty(characters);
		var inventory = saveGame.RestoreInventory(ContentRepository);
		var chest = saveGame.RestoreChestState(ContentRepository);
		var equipmentInstanceFactory = saveGame.RestoreEquipmentInstanceFactory();
		var currency = saveGame.RestoreCurrency();
		var clock = saveGame.RestoreClock();
		var location = saveGame.RestoreLocation();
		var mapEventProgress = saveGame.RestoreMapEventProgress();
		var shop = saveGame.RestoreShopState();
		var story = saveGame.RestoreStoryState();
		var journal = saveGame.RestoreJournal();
		var state = new GameState();
		state.SetAdventure(adventure);
		state.SetParty(party);
		state.SetInventory(inventory);
		state.SetChest(chest);
		state.SetEquipmentInstanceFactory(equipmentInstanceFactory);
		state.SetCurrency(currency);
		state.SetClock(clock);
		state.SetLocation(location);
		state.SetMapEventProgress(mapEventProgress);
		state.SetShop(shop);
		state.SetStory(story);
		state.SetJournal(journal);
		_session.ReplaceState(state);
		_session.Events.Publish(new SaveLoadedEvent());
		_logger.Info($"Loaded save game with {characters.Count} character(s).");
	}
}
