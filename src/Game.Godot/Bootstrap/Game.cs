using Game.Application;
using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Godot.Audio;

namespace Game.Godot;

public static class Game
{
	private static GameSession _currentSession = null!;
	private static IDiagnosticLogger _diagnosticLogger = null!;

	public static bool IsInitialized =>
		_currentSession is not null &&
		_diagnosticLogger is not null;

	public static GameSession Session
	{
		get
		{
			EnsureInitialized();
			return _currentSession;
		}
	}

	public static GameState State => Session.State;
	public static GameProfile Profile => Session.Profile;
	public static IContentRepository ContentRepository
	{
		get
		{
			EnsureInitialized();
			return _currentSession.ContentRepository;
		}
	}

	public static IDiagnosticLogger Logger
	{
		get
		{
			EnsureInitialized();
			return _diagnosticLogger;
		}
	}

	public static GameConfig Config
	{
		get
		{
			EnsureInitialized();
			return _currentSession.Config;
		}
	}

	public static SaveGameService SaveGameService => Session.SaveGameService;
	public static ProfileService ProfileService => Session.ProfileService;
	public static SessionFlowService SessionFlowService => Session.SessionFlowService;
	public static PartyService PartyService => Session.PartyService;
	public static InventoryService InventoryService => Session.InventoryService;
	public static ChestService ChestService => Session.ChestService;
	public static CharacterService CharacterService => Session.CharacterService;
	public static ItemUseService ItemUseService => Session.ItemUseService;
	public static ShopService ShopService => Session.ShopService;
	public static MapService MapService => Session.MapService;
	public static StoryService StoryService => Session.StoryService;
	public static AudioManager Audio => AudioManager.Instance;

	public static void Initialize(
		GameSession initialSession,
		IDiagnosticLogger? diagnosticLogger = null)
	{
		ArgumentNullException.ThrowIfNull(initialSession);
		if (initialSession.Config.InitialPartyCharacterIds.Count == 0)
		{
			throw new InvalidOperationException("Game initialization requires at least one initial party character.");
		}

		_currentSession = initialSession;
		_diagnosticLogger = diagnosticLogger ?? NullDiagnosticLogger.Instance;
		_diagnosticLogger.Info("Game initialized.");
	}

	public static void LoadSave(SaveGame saveGame)
	{
		EnsureInitialized();
		SaveGameService.LoadSave(saveGame);
	}

	private static void EnsureInitialized()
	{
		if (_currentSession is null || _diagnosticLogger is null)
		{
			throw new InvalidOperationException("Game has not been initialized.");
		}
	}
}
