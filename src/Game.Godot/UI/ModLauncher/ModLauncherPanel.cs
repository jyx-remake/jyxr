using Game.Application.Mods;
using Godot;

namespace Game.Godot.UI.ModLauncher;

public partial class ModLauncherPanel : Control
{
	private const string ClientVersion = "V0.1.0";
	private const string AndroidProjectDataRootPath = "/storage/emulated/0/JYXR";

	private TextureButton _refreshButton = null!;
	private ModShowcasePage _showcasePage = null!;
	private Label _clientVersionLabel = null!;
	private ProjectDataRoot _projectDataRoot = null!;
	private ColorRect _loadingOverlay = null!;
	private Label _loadingLabel = null!;
	private bool _isStarting = false;

	public override void _Ready()
	{
		_refreshButton = GetNode<TextureButton>("%LocalModButton");
		_showcasePage = GetNode<ModShowcasePage>("%ModShowcasePage");
		_clientVersionLabel = GetNode<Label>("%ClientVersionLabel");
		_loadingOverlay = GetNode<ColorRect>("%LoadingOverlay");
		_loadingLabel = _loadingOverlay.GetNode<Label>("LoadingLabel");

		_clientVersionLabel.Text = $"XR客户端版本: {ClientVersion}";
		_refreshButton.Pressed += RefreshMods;
		_showcasePage.StartRequested += OnStartRequested;

		_projectDataRoot = ProjectDataRoot.FromPath(ResolveProjectDataRootPath());
		RefreshMods();

		_loadingOverlay.Visible = false;
		_loadingLabel.Visible = false;
	}

	private void RefreshMods()
	{
		if (!Directory.Exists(_projectDataRoot.ModsDirectoryPath))
		{
			_showcasePage.Configure([]);
			return;
		}

		var mods = new ModRegistry(_projectDataRoot).DiscoverMods();
		_showcasePage.Configure(mods);

		if (mods.Count == 0)
		{
			GD.PushWarning($"No valid mods found under '{_projectDataRoot.ModsDirectoryPath}'.");
		}
	}

	private async void OnStartRequested(ModContext context)
	{
		if (_isStarting) return;
		_isStarting = true;

		try
		{
			_loadingOverlay.Visible = true;
			_loadingLabel.Visible = true;

			var scenePath = GameFlow.MainMenuScenePath;
			var loadResult = ResourceLoader.LoadThreadedRequest(scenePath);
			if (loadResult != Error.Ok)
				throw new Exception($"Failed to start loading scene: {loadResult}");

			while (ResourceLoader.LoadThreadedGetStatus(scenePath) == ResourceLoader.ThreadLoadStatus.InProgress)
			{
				await ToSignal(GetTree(), "process_frame");
			}

			var packedScene = ResourceLoader.LoadThreadedGet(scenePath) as PackedScene;
			if (packedScene == null)
				throw new Exception("Loaded scene is not a valid PackedScene");

			GameRuntimeBootstrap.Initialize(context, GetTree());
			SaveLauncherSettings(context);

			var error = GetTree().ChangeSceneToPacked(packedScene);
			if (error != Error.Ok)
				throw new InvalidOperationException($"Changing to main menu failed: {error}.");
		}
		catch (Exception ex)
		{
			_loadingOverlay.Visible = false;
			_loadingLabel.Visible = false;
			OS.Alert(ex.Message, "MOD 启动失败");
			_isStarting = false;
		}
	}

	private void SaveLauncherSettings(ModContext context)
	{
		var store = new LauncherSettingsStore(_projectDataRoot.LauncherSettingsPath);
		store.Save(new LauncherSettingsRecord(
			LauncherSettingsRecord.CurrentVersion,
			context.ModId));
	}

	private static string ResolveProjectDataRootPath()
	{
		if (OS.HasFeature("editor"))
			return ProjectSettings.GlobalizePath("res://");
		if (OS.HasFeature("android") || OS.HasFeature("web_android"))
			return AndroidProjectDataRootPath;
		return Path.GetDirectoryName(OS.GetExecutablePath()) ?? OS.GetUserDataDir();
	}
}