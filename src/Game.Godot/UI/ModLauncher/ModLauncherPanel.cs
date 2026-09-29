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
		_showcasePage.RefreshRequested += RefreshMods;

		_projectDataRoot = ProjectDataRootResolver.Resolve();
		var permissionHint = OS.HasFeature("android") || OS.HasFeature("web_android")
			? "权限：在系统设置中搜索“所有文件访问权限”，找到本游戏并开启“允许管理所有文件”。仅安装游戏不会自动授权。"
			: "权限：确认当前用户可以读取 MOD 目录和其中的文件。";
		_showcasePage.SetEmptyStateMessage(
			$"未找到可用的 MOD，请依次检查：\n\n" +
			$"版本：当前客户端 v{clientVersion}，请使用与该客户端匹配的 MOD 和资源包。\n\n" +
			$"{permissionHint}\n\n" +
			$"文件：请将完整 MOD 解压到下方目录，避免多套一层文件夹；每个 MOD 文件夹内应直接包含 mod.json，主 MOD 还需包含 data 文件夹和所需资源包。\n\n" +
			$"扫描目录：{_projectDataRoot.ModsDirectoryPath}\n\n" +
			"处理后点击“重新扫描”。");
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
		if (_isStarting) return;
		try
		{
			var mods = new ModRegistry(_projectDataRoot).DiscoverMods();
			var settings = new LauncherSettingsStore(_projectDataRoot.LauncherSettingsPath).LoadOrEmpty();
			_showcasePage.Configure(mods, settings);
		}
		catch (Exception exception)
		{
			_showcasePage.Configure([], LauncherSettingsRecord.Empty);
			GD.PushError(exception.ToString());
			_showcasePage.ShowLoadError(FormatLoadError(exception));
		}
	}

	private static string FormatLoadError(Exception exception) => exception switch
	{
		UnauthorizedAccessException => "文件访问被拒绝，请检查权限设置后重新扫描。",
		FileNotFoundException or DirectoryNotFoundException => "缺少所需文件，请完整解压 MOD 和配套资源包后重新扫描。",
		_ => $"加载失败，请检查客户端与 MOD 版本是否匹配、文件是否完整。\n详细原因：{exception.Message}",
	};

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
			_showcasePage.ShowLoadError(FormatLoadError(exception));
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
