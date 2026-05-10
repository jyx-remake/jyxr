using Game.Application;
using Game.Core.Story;
using Game.Godot.Persistence;
using Game.Godot.Settings;
using Godot;

namespace Game.Godot.UI;

public partial class SystemPanel : Control
{
	private const int MaxConsoleLineCount = 8;
	private const int MinBattleSpeedMultiplier = 1;
	private const int MaxBattleSpeedMultiplier = 5;

	private readonly LocalUserSettingsStore _settingsStore = new();
	private readonly List<string> _consoleLines = [];

	private UserSettingsRecord _settings = UserSettingsRecord.Default;
	private CheckBox _showBattleHpCheckBox = null!;
	private CheckBox _autoSaveCheckBox = null!;
	private CheckBox _autoBattleCheckBox = null!;
	private CheckBox _battleSpeedUpCheckBox = null!;
	private HSlider _battleSpeedMultiplierSlider = null!;
	private Label _battleSpeedMultiplierValueLabel = null!;
	private CheckBox _musicCheckBox = null!;
	private CheckBox _sfxCheckBox = null!;
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

		_showBattleHpCheckBox = GetNode<CheckBox>("%ShowBattleHpCheckBox");
		_autoSaveCheckBox = GetNode<CheckBox>("%AutoSaveCheckBox");
		_autoBattleCheckBox = GetNode<CheckBox>("%AutoBattleCheckBox");
		_battleSpeedUpCheckBox = GetNode<CheckBox>("%BattleSpeedUpCheckBox");
		_battleSpeedMultiplierSlider = GetNode<HSlider>("%BattleSpeedMultiplierSlider");
		_battleSpeedMultiplierValueLabel = GetNode<Label>("%BattleSpeedMultiplierValueLabel");
		_musicCheckBox = GetNode<CheckBox>("%MusicCheckBox");
		_sfxCheckBox = GetNode<CheckBox>("%SfxCheckBox");

		_executeButton.Pressed += OnExecutePressed;
		_consoleInput.TextSubmitted += OnConsoleTextSubmitted;
		_backButton.Pressed += () => UIRoot.Instance.CloseMainPanel();
		_quitButton.Pressed += OnQuitPressed;
		_loadButton.Pressed += OnLoadPressed;
		_saveButton.Pressed += OnSavePressed;
		_deleteSaveButton.Pressed += OnDeleteSavePressed;

		_showBattleHpCheckBox.Toggled += enabled => OnSettingToggled("战斗血条显示", enabled);
		_autoSaveCheckBox.Toggled += enabled => OnSettingToggled("自动存档", enabled);
		_autoBattleCheckBox.Toggled += enabled => OnSettingToggled("自动战斗", enabled);
		_battleSpeedUpCheckBox.Toggled += enabled => OnSettingToggled("战斗加速", enabled);
		_battleSpeedMultiplierSlider.ValueChanged += OnBattleSpeedMultiplierChanged;
		_musicCheckBox.Toggled += enabled => OnSettingToggled("音乐", enabled);
		_sfxCheckBox.Toggled += enabled => OnSettingToggled("音效", enabled);

		LoadSettings();
		AppendConsoleLine("系统", "命令行执行剧本指令，当前不支持 jump。");
		AppendConsoleLine("系统", "示例：item 道口烧鸡 / log \"踏入江湖\"");
		_consoleInput.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void LoadSettings()
	{
		_settings = _settingsStore.LoadOrDefault();
		ApplySettingsToControls(_settings);
		UserSettingsApplier.Apply(_settings);
	}

	private void SaveSettings(UserSettingsRecord settings)
	{
		_settingsStore.Save(settings);
	}

	private void OnSettingToggled(string displayName, bool enabled)
	{
		try
		{
			_settings = ReadSettingsFromControls();
			UserSettingsApplier.Apply(_settings);
			SaveSettings(_settings);
			AppendConsoleLine("设置", $"{displayName}：{(enabled ? "开启" : "关闭")}");
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Failed to apply setting '{displayName}'.", exception);
			AppendConsoleLine("错误", exception.Message);
		}
	}

	private void ApplySettingsToControls(UserSettingsRecord settings)
	{
		_showBattleHpCheckBox.SetPressedNoSignal(settings.ShowBattleHp);
		_autoSaveCheckBox.SetPressedNoSignal(settings.AutoSave);
		_autoBattleCheckBox.SetPressedNoSignal(settings.AutoBattle);
		_battleSpeedUpCheckBox.SetPressedNoSignal(settings.BattleSpeedUp);
		_battleSpeedMultiplierSlider.SetValueNoSignal(ClampBattleSpeedMultiplier(settings.BattleSpeedMultiplier));
		UpdateBattleSpeedMultiplierLabel((int)_battleSpeedMultiplierSlider.Value);
		_musicCheckBox.SetPressedNoSignal(settings.MusicEnabled);
		_sfxCheckBox.SetPressedNoSignal(settings.SfxEnabled);
	}

	private void OnBattleSpeedMultiplierChanged(double value)
	{
		var multiplier = ClampBattleSpeedMultiplier((int)Math.Round(value));
		_battleSpeedMultiplierSlider.SetValueNoSignal(multiplier);
		UpdateBattleSpeedMultiplierLabel(multiplier);
		if (_settings.BattleSpeedMultiplier == multiplier)
		{
			return;
		}
		_settings = ReadSettingsFromControls();
		UserSettingsApplier.Apply(_settings);
		SaveSettings(_settings);
	}

	private UserSettingsRecord ReadSettingsFromControls() => new(
		UserSettingsRecord.CurrentVersion,
		_showBattleHpCheckBox.ButtonPressed,
		_autoSaveCheckBox.ButtonPressed,
		_autoBattleCheckBox.ButtonPressed,
		_battleSpeedUpCheckBox.ButtonPressed,
		ClampBattleSpeedMultiplier((int)Math.Round(_battleSpeedMultiplierSlider.Value)),
		_musicCheckBox.ButtonPressed,
		_sfxCheckBox.ButtonPressed);

	private void UpdateBattleSpeedMultiplierLabel(int multiplier)
	{
		_battleSpeedMultiplierValueLabel.Text = $"{multiplier}倍";
	}

	private static int ClampBattleSpeedMultiplier(int multiplier) =>
		Math.Clamp(multiplier, MinBattleSpeedMultiplier, MaxBattleSpeedMultiplier);

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
