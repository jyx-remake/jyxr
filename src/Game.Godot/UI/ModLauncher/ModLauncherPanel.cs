using Game.Application;
using Game.Application.Mods;
using Godot;
using GodotFileAccess = global::Godot.FileAccess;

namespace Game.Godot.UI.ModLauncher;

public partial class ModLauncherPanel : Control
{
	private const string ClientAuthor = "虹乡俗人";
	private const string ClientVersionSetting = "application/config/version";
	private const string StoragePermission = "android.permission.MANAGE_EXTERNAL_STORAGE";
	private readonly IDiagnosticLogger _logger = new GodotDiagnosticLogger(GD.Print, GD.PushWarning, GD.PushError);

	private ModShowcasePage _showcasePage = null!;
	private Button _startLoadoutButton = null!;
	private Button _importModButton = null!;
	private FileDialog _importModDialog = null!;
	private Label _clientVersionLabel = null!;
	private ColorRect _loadingOverlay = null!;
	private Label _loadingTitleLabel = null!;
	private ProjectDataRoot _projectDataRoot = null!;
	private string _modSetupMessage = string.Empty;
	private bool _waitingForStoragePermission;
	private bool _isBusy;

	public override void _Ready()
	{
		_showcasePage = GetNode<ModShowcasePage>("%ModShowcasePage");
		_startLoadoutButton = GetNode<Button>("%StartLoadoutButton");
		_importModButton = GetNode<Button>("%ImportModButton");
		_importModDialog = GetNode<FileDialog>("%ImportModDialog");
		_clientVersionLabel = GetNode<Label>("%ClientVersionLabel");
		_loadingOverlay = GetNode<ColorRect>("%LoadingOverlay");
		_loadingTitleLabel = GetNode<Label>("%LoadingTitleLabel");

		var clientVersion = ProjectSettings.GetSetting(ClientVersionSetting).AsString();
		_clientVersionLabel.Text = $"XR 客户端 v{clientVersion} · 重制：{ClientAuthor} · 原作：汉家松鼠";
		_startLoadoutButton.Pressed += OnStartPressed;
		_importModButton.Visible = OS.HasFeature("android");
		_importModButton.Pressed += OnImportPressed;
		_importModDialog.FileSelected += OnImportFileSelected;
		_showcasePage.SelectionChanged += RefreshActions;
		_showcasePage.RefreshRequested += OnRefreshRequested;

		_projectDataRoot = ProjectDataRootResolver.Resolve();
		var permissionHint = OS.HasFeature("android") || OS.HasFeature("web_android")
			? "权限：需要开启“允许管理所有文件”，才能读取 MOD 和保存游戏数据。"
			: "权限：确认当前用户可以读取 MOD 目录和其中的文件。";
		_modSetupMessage =
			$"未找到可用的 MOD，请依次检查：\n\n" +
			$"版本：当前客户端 v{clientVersion}，请使用与该客户端匹配的 MOD 和资源包。\n\n" +
			$"{permissionHint}\n\n" +
			$"文件：请将完整 MOD 解压到下方目录，避免多套一层文件夹；每个 MOD 文件夹内应直接包含 mod.json，主 MOD 还需包含 data 文件夹和所需资源包。\n\n" +
			$"扫描目录：{_projectDataRoot.ModsDirectoryPath}\n\n" +
			"处理后点击“重新扫描”。";
		OnRefreshRequested();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusIn && IsNodeReady() && !_isBusy && _waitingForStoragePermission)
		{
			RefreshMods();
		}
	}

	private static bool HasStoragePermission() =>
		!OS.HasFeature("android") || OS.GetGrantedPermissions().Contains(StoragePermission);

	private void OnRefreshRequested()
	{
		if (_isBusy) return;
		if (HasStoragePermission())
		{
			RefreshMods();
			return;
		}

		ShowStoragePermissionRequired();
		_waitingForStoragePermission = true;
		if (OS.RequestPermission(StoragePermission))
		{
			RefreshMods();
		}
	}

	private void ShowStoragePermissionRequired()
	{
		_showcasePage.SetEmptyStateMessage(
			"需要文件访问权限\n\n" +
			"请在系统授权页面开启“允许管理所有文件”，返回游戏后将自动扫描 MOD。\n\n" +
			"如果尚未授权，请点击“前往授权”。\n\n" +
			$"数据目录：{_projectDataRoot.Path}");
		_showcasePage.SetRefreshButtonText("前往授权");
		_showcasePage.Configure([], LauncherSettingsRecord.Empty);
	}

	private void OnStartPressed()
	{
		if (_showcasePage.ResolvedLoadout is { } loadout)
		{
			OnStartRequested(loadout);
		}
	}

	private void RefreshActions()
	{
		_startLoadoutButton.Disabled = _showcasePage.ResolvedLoadout is null || _isBusy;
		_importModButton.Disabled = _isBusy || !HasStoragePermission();
	}

	private void SetBusy(bool busy, string? title = null)
	{
		_isBusy = busy;
		if (title is not null) _loadingTitleLabel.Text = title;
		_loadingOverlay.Visible = busy;
		RefreshActions();
	}

	private void OnImportPressed()
	{
		if (_isBusy) return;
		if (!HasStoragePermission())
		{
			OnRefreshRequested();
			return;
		}
		_importModDialog.PopupCenteredRatio();
	}

	private async void OnImportFileSelected(string path)
	{
		if (_isBusy) return;
		SetBusy(true, "正在导入 MOD，请稍候…");
		try
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			var ids = await Task.Run(() => ImportArchive(path));
			if (RefreshMods())
				_showcasePage.ShowMessage($"导入完成：{string.Join("、", ids)}", isError: false);
		}
		catch (Exception exception)
		{
			_logger.Error("Importing MOD archive failed.", exception);
			_showcasePage.ShowLoadError($"导入失败：{exception.Message}");
		}
		finally
		{
			SetBusy(false);
		}
	}

	private IReadOnlyList<string> ImportArchive(string path)
	{
		// Android's system picker returns content:// URIs; System.IO cannot open them.
		using var source = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Read)
			?? throw new IOException($"无法读取所选 ZIP：{GodotFileAccess.GetOpenError()}");
		Directory.CreateDirectory(_projectDataRoot.LauncherDirectoryPath);
		var archivePath = Path.Combine(_projectDataRoot.LauncherDirectoryPath, $"archive-{Guid.NewGuid():N}.zip");
		using var archive = new FileStream(archivePath, FileMode.CreateNew, System.IO.FileAccess.ReadWrite,
			FileShare.None, bufferSize: 81920, FileOptions.DeleteOnClose);
		while (true)
		{
			var buffer = source.GetBuffer(1024 * 1024);
			if (buffer.Length == 0) break;
			archive.Write(buffer);
		}
		if (source.GetError() is not (Error.Ok or Error.FileEof))
			throw new IOException("读取 ZIP 文件失败。");
		archive.Position = 0;
		return new ModArchiveImporter(_projectDataRoot).Import(archive);
	}

	private bool RefreshMods()
	{
		if (!HasStoragePermission())
		{
			ShowStoragePermissionRequired();
			return false;
		}

		_waitingForStoragePermission = false;
		_showcasePage.SetEmptyStateMessage(_modSetupMessage);
		_showcasePage.SetRefreshButtonText("重新扫描");
		try
		{
			var mods = new ModRegistry(_projectDataRoot).DiscoverMods();
			var settings = new LauncherSettingsStore(_projectDataRoot.LauncherSettingsPath).LoadOrEmpty();
			_showcasePage.Configure(mods, settings);
			return true;
		}
		catch (Exception exception)
		{
			_showcasePage.Configure([], LauncherSettingsRecord.Empty);
			GD.PushError(exception.ToString());
			_showcasePage.ShowLoadError(FormatLoadError(exception));
			return false;
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
		if (_isBusy)
		{
			return;
		}

		SetBusy(true, $"正在加载《{loadout.PrimaryMod.Manifest.Name.Trim()}》与 {loadout.AddonMods.Count} 个扩展");

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
			SetBusy(false);
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
