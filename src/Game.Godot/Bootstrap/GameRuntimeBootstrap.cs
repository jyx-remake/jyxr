using Game.Application;
using Game.Application.Mods;
using Game.Content.Loading;
using Game.Core.Model;
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
	private const string RuntimeRootName = "__GameRuntime";
	private const string WorldScenePath = "res://autoload/world.tscn";
	private const string UIRootScenePath = "res://autoload/ui_root.tscn";
	private const string AudioManagerScenePath = "res://autoload/audio_manager.tscn";

	private static DiagnosticLogger? _logger;
	private static ModLoadout? _activeModLoadout;

	public static ModLoadout ActiveModLoadout =>
		_activeModLoadout ?? throw new InvalidOperationException("No active mod loadout has been bootstrapped.");

	public static void Initialize(ModLoadout modLoadout, SceneTree sceneTree)
	{
		ArgumentNullException.ThrowIfNull(modLoadout);
		ArgumentNullException.ThrowIfNull(sceneTree);

		_activeModLoadout = modLoadout;
		var logger = EnsureLogger();
		LoadResourcePacks(modLoadout, logger);
		EnsureRuntimeNodes(sceneTree);

		var contentInputs = modLoadout.ModsInLoadOrder
			.Select((mod, index) => new ModContentInput(mod.ModId, mod.ModDirectoryPath, index == 0))
			.ToArray();
		var loadedContent = new JsonContentLoader().LoadModContent(contentInputs);
		foreach (var warning in loadedContent.Report.Warnings)
		{
			logger.Warning(warning.Message);
		}

		var config = loadedContent.Config;
		var repository = loadedContent.Repository;
		var settingsStore = new LocalUserSettingsStore(modLoadout.StoragePaths.SettingsPath, logger);
		var settings = settingsStore.LoadOrDefault();
		var userSettings = new UserSettingsService(settingsStore, settings);
		var profile = new LocalProfileStore(modLoadout.StoragePaths.ProfilePath, logger).LoadOrEmpty().Restore();
		var session = BuildSession(repository, logger, config, profile);

		Game.Initialize(session, modLoadout, userSettings, logger);
		userSettings.ApplyCurrent();
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

	private static DiagnosticLogger EnsureLogger() =>
		_logger ??= new GodotDiagnosticLogger(GD.Print, GD.PushWarning, GD.PushError);

	private static void LoadResourcePacks(ModLoadout modLoadout, DiagnosticLogger logger)
	{
		foreach (var modContext in modLoadout.ModsInLoadOrder)
		{
			foreach (var packFilePath in modContext.PackFilePaths)
			{
				if (!ProjectSettings.LoadResourcePack(packFilePath, replaceFiles: true))
				{
					throw new InvalidOperationException(
						$"Failed to load resource pack '{packFilePath}' from mod '{modContext.ModId}'.");
				}

				logger.Info($"Loaded resource pack from mod '{modContext.ModId}': {packFilePath}");
			}
		}
	}

	private static void BindUiToSession(ApplicationGameSession session)
	{
		UIRoot.Instance.BindSessionEvents(session);
		World.Instance.GetNode<TimedStoryCoordinator>("%TimedStoryCoordinator").Bind(session);
		World.Instance.GetNode<WorldTriggerCoordinator>("%WorldTriggerCoordinator").Bind(session);
		World.Instance.AutoSave.Bind(session);
		World.Instance.PlayTime.Bind(session);
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
