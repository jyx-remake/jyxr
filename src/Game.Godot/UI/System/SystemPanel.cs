using Game.Application;
using Game.Core.Story;
using Godot;

namespace Game.Godot.UI;

public partial class SystemPanel : Control
{
	private const string SettingsFilePath = "user://settings.cfg";
	private const string SettingsSection = "system_panel";
	private const string ShowBattleHpKey = "show_battle_hp";
	private const string AutoSaveKey = "auto_save";
	private const string AutoBattleKey = "auto_battle";
	private const string MusicEnabledKey = "music_enabled";
	private const string SfxEnabledKey = "sfx_enabled";
	private const string BgmBusName = "Bgm";
	private const string SfxBusName = "SFX";
	private const int MaxConsoleLineCount = 8;

	private readonly Dictionary<string, CheckBox> _checkboxesByKey = [];
	private readonly Dictionary<string, bool> _defaultSettings = [];
	private readonly List<string> _consoleLines = [];

	private LineEdit _consoleInput = null!;
	private RichTextLabel _consoleOutput = null!;
	private Button _executeButton = null!;
	private Button _backButton = null!;
	private Button _quitButton = null!;
	private Button _loadButton = null!;
	private Button _saveButton = null!;
	private Button _deleteSaveButton = null!;

	public override void _Ready()
	{
		_consoleInput = GetNode<LineEdit>("%ConsoleInput");
		_consoleOutput = GetNode<RichTextLabel>("%ConsoleOutput");
		_executeButton = GetNode<Button>("%ExecuteButton");
		_backButton = GetNode<Button>("%BackButton");
		_quitButton = GetNode<Button>("%QuitButton");
		_loadButton = GetNode<Button>("%LoadButton");
		_saveButton = GetNode<Button>("%SaveButton");
		_deleteSaveButton = GetNode<Button>("%DeleteSaveButton");

		RegisterSetting(ShowBattleHpKey, GetNode<CheckBox>("%ShowBattleHpCheckBox"));
		RegisterSetting(AutoSaveKey, GetNode<CheckBox>("%AutoSaveCheckBox"));
		RegisterSetting(AutoBattleKey, GetNode<CheckBox>("%AutoBattleCheckBox"));
		RegisterSetting(MusicEnabledKey, GetNode<CheckBox>("%MusicCheckBox"));
		RegisterSetting(SfxEnabledKey, GetNode<CheckBox>("%SfxCheckBox"));

		_executeButton.Pressed += OnExecutePressed;
		_consoleInput.TextSubmitted += OnConsoleTextSubmitted;
		_backButton.Pressed += () => UIRoot.Instance.CloseMainPanel();
		_quitButton.Pressed += OnQuitPressed;
		_loadButton.Pressed += OnLoadPressed;
		_saveButton.Pressed += OnSavePressed;
		_deleteSaveButton.Pressed += OnDeleteSavePressed;

		LoadSettings();
		AppendConsoleLine("系统", "命令行执行剧本指令，当前不支持 jump。");
		AppendConsoleLine("系统", "示例：item 道口烧鸡 / log \"踏入江湖\"");
	}

	private void RegisterSetting(string key, CheckBox checkBox)
	{
		_checkboxesByKey.Add(key, checkBox);
		_defaultSettings.Add(key, checkBox.ButtonPressed);
		checkBox.Toggled += enabled => OnSettingToggled(key, enabled);
	}

	private void LoadSettings()
	{
		var config = new ConfigFile();
		var loadResult = config.Load(SettingsFilePath);

		foreach (var (key, checkBox) in _checkboxesByKey)
		{
			var fallbackValue = _defaultSettings[key];
			var enabled = loadResult == Error.Ok
				? (bool)config.GetValue(SettingsSection, key, fallbackValue)
				: fallbackValue;

			checkBox.SetPressedNoSignal(enabled);
			ApplySetting(key, enabled);
		}

		if (loadResult != Error.Ok)
		{
			SaveSettings();
		}
	}

	private void SaveSettings()
	{
		var config = new ConfigFile();
		foreach (var (key, checkBox) in _checkboxesByKey)
		{
			config.SetValue(SettingsSection, key, checkBox.ButtonPressed);
		}

		var saveResult = config.Save(SettingsFilePath);
		if (saveResult != Error.Ok)
		{
			throw new InvalidOperationException($"保存设置失败：{saveResult}");
		}
	}

	private void OnSettingToggled(string key, bool enabled)
	{
		try
		{
			ApplySetting(key, enabled);
			SaveSettings();
			AppendConsoleLine("设置", $"{GetSettingDisplayName(key)}：{(enabled ? "开启" : "关闭")}");
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Failed to apply setting '{key}'.", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void ApplySetting(string key, bool enabled)
	{
		switch (key)
		{
			case MusicEnabledKey:
				ApplyBusEnabled(BgmBusName, enabled);
				return;
			case SfxEnabledKey:
				ApplyBusEnabled(SfxBusName, enabled);
				return;
			case ShowBattleHpKey:
			case AutoSaveKey:
			case AutoBattleKey:
				return;
			default:
				throw new InvalidOperationException($"Unknown system setting key: {key}");
		}
	}

	private static void ApplyBusEnabled(string busName, bool enabled)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			throw new InvalidOperationException($"音频总线不存在：{busName}");
		}

		AudioServer.SetBusMute(busIndex, !enabled);
	}

	private void OnExecutePressed() => SubmitConsoleCommand(_consoleInput.Text);

	private void OnConsoleTextSubmitted(string text) => SubmitConsoleCommand(text);

	private async void SubmitConsoleCommand(string text)
	{
		var commandLine = text.Trim();
		if (string.IsNullOrWhiteSpace(commandLine))
		{
			AppendConsoleLine("控制台", "请输入有效指令。");
			return;
		}

		_consoleInput.Clear();
		try
		{
			var invocation = await Game.StoryService.CommandLine.ExecuteAsync(commandLine);
			AppendConsoleLine("控制台", $"已执行剧本指令：{FormatInvocation(invocation)}");
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Console command failed: {commandLine}", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void OnSavePressed()
	{
		try
		{
			UIRoot.Instance.ShowSaveSlotSelectionPanel(SaveSlotPanelMode.Save);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Opening save slot panel failed.", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void OnLoadPressed()
	{
		try
		{
			UIRoot.Instance.ShowSaveSlotSelectionPanel(SaveSlotPanelMode.Load);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Opening load slot panel failed.", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void OnDeleteSavePressed()
	{
		try
		{
			UIRoot.Instance.ShowSaveSlotSelectionPanel(SaveSlotPanelMode.Delete);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Opening delete slot panel failed.", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void OnQuitPressed()
	{
		AppendConsoleLine("系统", "正在退出游戏。");
		GetTree().Quit();
	}

	private void AppendConsoleLine(string source, string message)
	{
		_consoleLines.Add($"[color=#513523]{source}[/color]  {message}");
		while (_consoleLines.Count > MaxConsoleLineCount)
		{
			_consoleLines.RemoveAt(0);
		}

		_consoleOutput.Clear();
		foreach (var line in _consoleLines)
		{
			_consoleOutput.AppendText(line + "\n");
		}
	}

	private static string GetSettingDisplayName(string key) =>
		key switch
		{
			ShowBattleHpKey => "战斗血条显示",
			AutoSaveKey => "自动存档",
			AutoBattleKey => "自动战斗",
			MusicEnabledKey => "音乐",
			SfxEnabledKey => "音效",
			_ => key,
		};

	private static string FormatInvocation(StoryCommandInvocation invocation)
	{
		if (invocation.Arguments.Count == 0)
		{
			return invocation.Name;
		}

		return $"{invocation.Name} {string.Join(" ", invocation.Arguments.Select(FormatArgument))}";
	}

	private static string FormatArgument(ExprValue value) =>
		value.Kind == ExprValueKind.String
			? $"\"{value.AsString("console")}\""
			: value.ToString();
}
