using Game.Application.Mods;
using Godot;

namespace Game.Godot.UI.ModLauncher;

public partial class ModLauncherPanel : Control
{
	private const string ClientAuthor = "虹乡俗人";
	private const string ClientVersionSetting = "application/config/version";

	private ModShowcasePage _showcasePage = null!;
	private Button _startLoadoutButton = null!;
	private Label _clientVersionLabel = null!;
	private ColorRect _loadingOverlay = null!;
	private Label _loadingTitleLabel = null!;
	private ProjectDataRoot _projectDataRoot = null!;
	private bool _isStarting;

	public override void _Ready()
	{
		_showcasePage = GetNode<ModShowcasePage>("%ModShowcasePage");
		_startLoadoutButton = GetNode<Button>("%StartLoadoutButton");
		_clientVersionLabel = GetNode<Label>("%ClientVersionLabel");
		_loadingOverlay = GetNode<ColorRect>("%LoadingOverlay");
		_loadingTitleLabel = GetNode<Label>("%LoadingTitleLabel");

		var clientVersion = ProjectSettings.GetSetting(ClientVersionSetting).AsString();
		_clientVersionLabel.Text = $"XR 客户端 v{clientVersion} · 重制：{ClientAuthor} · 原作：汉家松鼠";
		_startLoadoutButton.Pressed += OnStartPressed;
		_showcasePage.SelectionChanged += RefreshStartButton;

		_projectDataRoot = ProjectDataRootResolver.Resolve();
		RefreshMods();
	}

	private void OnStartPressed()
	{
		if (_showcasePage.ResolvedLoadout is { } loadout)
		{
			OnStartRequested(loadout);
		}
	}

	private void RefreshStartButton()
	{
		_startLoadoutButton.Disabled = _showcasePage.ResolvedLoadout is null || _isStarting;
	}

	private void RefreshMods()
	{
		if (!Directory.Exists(_projectDataRoot.ModsDirectoryPath))
		{
			_showcasePage.Configure([], LauncherSettingsRecord.Empty);
			return;
		}

		var mods = new ModRegistry(_projectDataRoot).DiscoverMods();
		var settings = new LauncherSettingsStore(_projectDataRoot.LauncherSettingsPath).LoadOrEmpty();
		_showcasePage.Configure(mods, settings);

		if (mods.Count == 0)
		{
			GD.PushWarning($"No valid mods found under '{_projectDataRoot.ModsDirectoryPath}'.");
		}
	}

	private async void OnStartRequested(ModLoadout loadout)
	{
		if (_isStarting)
		{
			return;
		}

		_isStarting = true;
		RefreshStartButton();
		_loadingTitleLabel.Text =
			$"正在加载《{loadout.PrimaryMod.Manifest.Name.Trim()}》与 {loadout.AddonMods.Count} 个扩展";
		_loadingOverlay.Show();

		try
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			GameRuntimeBootstrap.Initialize(loadout, GetTree());
			SaveLauncherSettings(loadout);
			var error = GetTree().ChangeSceneToFile(GameFlow.MainMenuScenePath);
			if (error != Error.Ok)
			{
				throw new InvalidOperationException($"Changing to main menu failed: {error}.");
			}
		}
		catch (Exception exception)
		{
			_loadingOverlay.Hide();
			_isStarting = false;
			RefreshStartButton();
			GD.PushError(exception.ToString());
			OS.Alert(exception.Message, "MOD 启动失败");
		}
	}

	private void SaveLauncherSettings(ModLoadout loadout)
	{
		var store = new LauncherSettingsStore(_projectDataRoot.LauncherSettingsPath);
		store.Save(new LauncherSettingsRecord(
			LauncherSettingsRecord.CurrentVersion,
			loadout.PrimaryMod.ModId,
			loadout.AddonMods.Select(static mod => mod.ModId).ToArray()));
	}

}
