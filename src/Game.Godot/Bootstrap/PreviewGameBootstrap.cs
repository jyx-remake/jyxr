using System.Text.Json;
using Game.Content.Loading;
using Game.Application;
using Game.Core.Model;
using Game.Core.Serialization;
using Game.Godot.Persistence;
using Game.Godot.Settings;
using Game.Godot.Story;
using Game.Godot.UI;
using Godot;
using ApplicationGameSession = Game.Application.GameSession;
using DiagnosticLogger = Game.Application.IDiagnosticLogger;

namespace Game.Godot;

public static class PreviewGameBootstrap
{
	private const string DataDirectoryPath = "res://data";
	private const string GameConfigFileName = "game-config.json";
	private static GodotContentPackageLoader? _contentPackageLoader;
	private static InMemoryContentRepository? _repository;
	private static DiagnosticLogger? _logger;
	private static GameConfig? _config;

	public static void Initialize()
	{
		var config = EnsureConfig();
		var logger = EnsureLogger();
		var repository = EnsureRepository();
		var session = BuildSession(repository, logger, config, new GameProfile());

		Game.Initialize(session, logger);
		ApplyPersistentUserSettings();
		LoadPersistentProfile();
		BindUiToSession(session);
	}

	public static void StartNewGame()
	{
		Game.SessionFlowService.StartNewGame();
	}

	private static InMemoryContentRepository LoadRepository()
	{
		var package = EnsureContentPackageLoader().Load();
		return new JsonContentLoader().LoadFromPackage(package);
	}

	private static ApplicationGameSession BuildSession(
		InMemoryContentRepository repository,
		DiagnosticLogger logger,
		GameConfig config,
		GameProfile profile)
	{
		var state = new NewGameStateFactory(repository, config).Create(config.InitialPartyCharacterIds);
		return new ApplicationGameSession(
			state,
			repository,
			new GodotStoryRuntimeHost(),
			logger,
			profile,
			config);
	}

	private static GameConfig LoadConfig()
	{
		var json = EnsureContentPackageLoader().ReadText(GameConfigFileName);
		var config = JsonSerializer.Deserialize<GameConfig>(json, GameJson.Default)
			?? throw new InvalidOperationException($"无法反序列化游戏配置文件: {GameConfigFileName}");
		if (string.IsNullOrWhiteSpace(config.InitialStorySegmentId))
		{
			throw new InvalidOperationException("游戏配置缺少 initialStorySegmentId。");
		}

		if (config.InitialPartyCharacterIds.Count == 0)
		{
			throw new InvalidOperationException("游戏配置缺少 initialPartyCharacterIds。");
		}

		return config;
	}

	private static DiagnosticLogger EnsureLogger() =>
		_logger ??= new GodotDiagnosticLogger(GD.Print, GD.PushWarning, GD.PushError);

	private static GodotContentPackageLoader EnsureContentPackageLoader() =>
		_contentPackageLoader ??= new GodotContentPackageLoader(DataDirectoryPath);

	private static GameConfig EnsureConfig() =>
		_config ??= LoadConfig();

	private static InMemoryContentRepository EnsureRepository() =>
		_repository ??= LoadRepository();

	private static void BindUiToSession(ApplicationGameSession session)
	{
		// TODO: 这里是 preview 启动链下的临时接线点。
		// 后续应迁回更合适的正式宿主装配位置，而不是长期放在 PreviewGameBootstrap 中。
		UIRoot.Instance.BindSessionEvents(session);
		World.Instance.GetNode<TimedStoryCoordinator>("%TimedStoryCoordinator").Bind(session);
	}

	private static void LoadPersistentProfile()
	{
		var profileStore = new LocalProfileStore();
		Game.ProfileService.LoadProfile(profileStore.LoadOrEmpty());
	}

	private static void ApplyPersistentUserSettings()
	{
		var settingsStore = new LocalUserSettingsStore();
		UserSettingsApplier.Apply(settingsStore.LoadOrDefault());
	}
}
