using System.Text.Json;
using Game.Application;
using Game.Application.Mods;
using Game.Content.Loading;
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

public static class GameRuntimeBootstrap
{
	private const string GameConfigFileName = "game-config.json";
	private const string RuntimeRootName = "__GameRuntime";
	private const string WorldScenePath = "res://autoload/world.tscn";
	private const string UIRootScenePath = "res://autoload/ui_root.tscn";
	private const string AudioManagerScenePath = "res://autoload/audio_manager.tscn";

	private static DiagnosticLogger? _logger;
	private static ModContext? _activeMod;

	public static ModContext ActiveMod =>
		_activeMod ?? throw new InvalidOperationException("No active mod has been bootstrapped.");

	public static void Initialize(ModContext modContext, SceneTree sceneTree)
	{
		ArgumentNullException.ThrowIfNull(modContext);
		ArgumentNullException.ThrowIfNull(sceneTree);

		_activeMod = modContext;
		var logger = EnsureLogger();
		LoadResourcePacks(modContext, logger);
		EnsureRuntimeNodes(sceneTree);

		var config = LoadConfig(modContext);
		var repository = new JsonContentLoader().LoadFromDirectory(modContext.DataDirectoryPath);
		var settings = new LocalUserSettingsStore(modContext.StoragePaths.SettingsPath, logger).LoadOrDefault();
		var profile = new LocalProfileStore(modContext.StoragePaths.ProfilePath, logger).LoadOrEmpty().Restore();
		var session = BuildSession(repository, logger, config, profile);

		Game.Initialize(session, modContext, logger);
		UserSettingsApplier.Apply(settings);
		BindUiToSession(session);
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

	private static GameConfig LoadConfig(ModContext modContext)
	{
		var configPath = Path.Combine(modContext.DataDirectoryPath, GameConfigFileName);
		if (!File.Exists(configPath))
		{
			throw new FileNotFoundException($"Mod game config was not found: {configPath}", configPath);
		}

		var json = File.ReadAllText(configPath);
		var config = JsonSerializer.Deserialize<GameConfig>(json, GameJson.Default)
			?? throw new InvalidOperationException($"无法反序列化游戏配置文件: {configPath}");
		if (string.IsNullOrWhiteSpace(config.InitialStorySegmentId))
		{
			throw new InvalidOperationException("游戏配置缺少 initialStorySegmentId。");
		}

		if (config.InitialPartyCharacterIds.Count == 0)
		{
			throw new InvalidOperationException("游戏配置缺少 initialPartyCharacterIds。");
		}

		if (config.SelectablePortraitIds.Count == 0)
		{
			throw new InvalidOperationException("游戏配置缺少 selectablePortraitIds。");
		}

		return config;
	}

	private static DiagnosticLogger EnsureLogger() =>
		_logger ??= new GodotDiagnosticLogger(GD.Print, GD.PushWarning, GD.PushError);

	private static void LoadResourcePacks(ModContext modContext, DiagnosticLogger logger)
	{
		foreach (var packFilePath in modContext.PackFilePaths)
		{
			if (!ProjectSettings.LoadResourcePack(packFilePath, replaceFiles: true))
			{
				throw new InvalidOperationException($"Failed to load mod resource pack: {packFilePath}");
			}

			logger.Info($"Loaded mod resource pack: {packFilePath}");
		}
	}

	private static void BindUiToSession(ApplicationGameSession session)
	{
		UIRoot.Instance.BindSessionEvents(session);
		World.Instance.GetNode<TimedStoryCoordinator>("%TimedStoryCoordinator").Bind(session);
		World.Instance.AutoSave.Bind(session);
	}

	private static void EnsureRuntimeNodes(SceneTree sceneTree)
	{
		var root = sceneTree.Root.GetNodeOrNull<Node>(RuntimeRootName);
		if (root is not null)
		{
			return;
		}

		root = new Node { Name = RuntimeRootName };
		sceneTree.Root.AddChild(root);
		root.AddChild(InstantiateRequired(WorldScenePath, "World"));
		root.AddChild(InstantiateRequired(UIRootScenePath, "UIRoot"));
		root.AddChild(InstantiateRequired(AudioManagerScenePath, "AudioManager"));
	}

	private static Node InstantiateRequired(string scenePath, string description)
	{
		var scene = GD.Load<PackedScene>(scenePath)
			?? throw new InvalidOperationException($"Runtime scene could not be loaded: {scenePath}");
		return scene.Instantiate() as Node
			?? throw new InvalidOperationException($"Runtime scene root must be Node: {description}");
	}
}
