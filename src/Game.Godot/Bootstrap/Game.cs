using Game.Application;
using Game.Application.Mods;
using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Godot.Audio;
using Godot;

namespace Game.Godot;

public enum GameClientPlatformKind
{
	Desktop,
	Mobile
}

public static class Game
{
	private static GameSession _currentSession = null!;
	private static IDiagnosticLogger _diagnosticLogger = null!;
	private static ModContext _activeMod = null!;

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

	public static GameSettings Settings
	{
		get
		{
			EnsureInitialized();
			return _currentSession.Settings;
		}
	}

	public static SaveGameService SaveGameService => Session.SaveGameService;
	public static ProfileService ProfileService => Session.ProfileService;
	public static SkillMaxLevelPolicy SkillMaxLevelPolicy => Session.SkillMaxLevelPolicy;
	public static CharacterResourceLimitPolicy CharacterResourceLimitPolicy => Session.CharacterResourceLimitPolicy;
	public static SessionFlowService SessionFlowService => Session.SessionFlowService;
	public static PartyService PartyService => Session.PartyService;
	public static InventoryService InventoryService => Session.InventoryService;
	public static ChestService ChestService => Session.ChestService;
	public static CharacterService CharacterService => Session.CharacterService;
	public static ItemUseService ItemUseService => Session.ItemUseService;
	public static ShopService ShopService => Session.ShopService;
	public static BattleService BattleService => Session.BattleService;
	public static SpecialBattleService SpecialBattleService => Session.SpecialBattleService;
	public static MiniGameService MiniGameService => Session.MiniGameService;
	public static WorldTriggerService WorldTriggerService => Session.WorldTriggerService;
	public static MapService MapService => Session.MapService;
	public static StoryService StoryService => Session.StoryService;
	public static AudioManager Audio => AudioManager.Instance;
	public static ModContext ActiveMod
	{
		get
		{
			EnsureInitialized();
			return _activeMod;
		}
	}

	public static GameClientPlatformKind ClientPlatform => ResolveClientPlatform();
	public static bool IsDesktopPlatform => ClientPlatform == GameClientPlatformKind.Desktop;
	public static bool IsMobilePlatform => ClientPlatform == GameClientPlatformKind.Mobile;

	public static void Initialize(
		GameSession initialSession,
		ModContext activeMod,
		IDiagnosticLogger? diagnosticLogger = null)
	{
		ArgumentNullException.ThrowIfNull(initialSession);
		ArgumentNullException.ThrowIfNull(activeMod);
		if (initialSession.Config.InitialPartyCharacterIds.Count == 0)
		{
			throw new InvalidOperationException("Game initialization requires at least one initial party character.");
		}

		_currentSession = initialSession;
		_activeMod = activeMod;
		_diagnosticLogger = diagnosticLogger ?? NullDiagnosticLogger.Instance;
		_diagnosticLogger.Info($"Game initialized with mod '{activeMod.ModId}'.");
	}

	private static void EnsureInitialized()
	{
		if (_currentSession is null || _diagnosticLogger is null)
		{
			throw new InvalidOperationException("Game has not been initialized.");
		}
	}

	private static GameClientPlatformKind ResolveClientPlatform()
	{
		if (OS.HasFeature("android") ||
			OS.HasFeature("ios") ||
			OS.HasFeature("web_android") ||
			OS.HasFeature("web_ios"))
		{
			return GameClientPlatformKind.Mobile;
		}

		return GameClientPlatformKind.Desktop;
	}
}
