using Game.Application;
using Game.Godot.Persistence;
using Game.Godot.Settings;
using Godot;

namespace Game.Godot.UI;

public partial class SettingsSection : Control
{
	private const int MinBattleSpeedMultiplier = 1;
	private const int MaxBattleSpeedMultiplier = 5;
	private static readonly IReadOnlyList<(string Label, ScreenAspectMode Value)> ScreenAspectOptions =
	[
		("无限制", ScreenAspectMode.Unlimited),
		("16:9", ScreenAspectMode.Ratio16x9),
		("18:9", ScreenAspectMode.Ratio18x9),
		("20:9", ScreenAspectMode.Ratio20x9),
	];
	private static readonly IReadOnlyList<(string Label, WindowDisplayMode Value)> WindowDisplayOptions =
	[
		("窗口", WindowDisplayMode.Windowed),
		("全屏", WindowDisplayMode.Fullscreen),
	];

	private CheckBox _showBattleBoardCheckBox = null!;
	private CheckBox _showBattleHpCheckBox = null!;
	private CheckBox _autoSaveCheckBox = null!;
	private CheckBox _largeMapMovementAnimationCheckBox = null!;
	private CheckBox _autoBattleCheckBox = null!;
	private CheckBox _hideBattleHudDuringAutomatedTurnsCheckBox = null!;
	private CheckBox _battleSpeedUpCheckBox = null!;
	private CheckBox _typewriterDialogCheckBox = null!;
	private HSlider _battleSpeedMultiplierSlider = null!;
	private Label _battleSpeedMultiplierValueLabel = null!;
	private OptionButton _screenAspectOptionButton = null!;
	private Control _windowDisplayRow = null!;
	private OptionButton _windowDisplayOptionButton = null!;
	private CheckBox _musicCheckBox = null!;
	private CheckBox _sfxCheckBox = null!;
	private IDisposable? _adventureStateSubscription;

	public override void _Ready()
	{
		_showBattleBoardCheckBox = GetNode<CheckBox>("%ShowBattleBoardCheckBox");
		_showBattleHpCheckBox = GetNode<CheckBox>("%ShowBattleHpCheckBox");
		_autoSaveCheckBox = GetNode<CheckBox>("%AutoSaveCheckBox");
		_largeMapMovementAnimationCheckBox = GetNode<CheckBox>("%LargeMapMovementAnimationCheckBox");
		_autoBattleCheckBox = GetNode<CheckBox>("%AutoBattleCheckBox");
		_hideBattleHudDuringAutomatedTurnsCheckBox = GetNode<CheckBox>("%HideBattleHudDuringAutomatedTurnsCheckBox");
		_battleSpeedUpCheckBox = GetNode<CheckBox>("%BattleSpeedUpCheckBox");
		_typewriterDialogCheckBox = GetNode<CheckBox>("%TypewriterDialogCheckBox");
		_battleSpeedMultiplierSlider = GetNode<HSlider>("%BattleSpeedMultiplierSlider");
		_battleSpeedMultiplierValueLabel = GetNode<Label>("%BattleSpeedMultiplierValueLabel");
		_screenAspectOptionButton = GetNode<OptionButton>("%ScreenAspectOptionButton");
		_windowDisplayRow = GetNode<Control>("%WindowDisplayRow");
		_windowDisplayOptionButton = GetNode<OptionButton>("%WindowDisplayOptionButton");
		_musicCheckBox = GetNode<CheckBox>("%MusicCheckBox");
		_sfxCheckBox = GetNode<CheckBox>("%SfxCheckBox");

		_showBattleBoardCheckBox.Toggled += _ => UpdateSettings("战斗棋盘显示");
		_showBattleHpCheckBox.Toggled += _ => UpdateSettings("战斗血条显示");
		_autoSaveCheckBox.Toggled += _ => UpdateSettings("自动存档");
		_largeMapMovementAnimationCheckBox.Toggled += _ => UpdateSettings("大地图移动动画");
		_autoBattleCheckBox.Toggled += _ => UpdateSettings("自动战斗");
		_hideBattleHudDuringAutomatedTurnsCheckBox.Toggled += _ => UpdateSettings("战场自动行动时隐藏操作栏");
		_battleSpeedUpCheckBox.Toggled += _ => UpdateSettings("战斗加速");
		_typewriterDialogCheckBox.Toggled += _ => UpdateSettings("对话逐字显示");
		_musicCheckBox.Toggled += _ => UpdateSettings("音乐");
		_sfxCheckBox.Toggled += _ => UpdateSettings("音效");
		_battleSpeedMultiplierSlider.ValueChanged += OnBattleSpeedMultiplierChanged;
		_screenAspectOptionButton.ItemSelected += _ => UpdateSettings("画面尺寸");
		_windowDisplayOptionButton.ItemSelected += _ => UpdateSettings("窗口模式");

		OptionButtonBinder.PopulateEnum(_screenAspectOptionButton, ScreenAspectOptions);
		OptionButtonBinder.PopulateEnum(_windowDisplayOptionButton, WindowDisplayOptions);
		_windowDisplayRow.Visible = Game.IsDesktopPlatform;
		ApplySettingsToControls(Game.UserSettings.Current);
		ApplyNoRegretRestrictions();
		_adventureStateSubscription = Game.Session.Events.Subscribe<AdventureStateChangedEvent>(_ => ApplyNoRegretRestrictions());
	}

	public override void _ExitTree()
	{
		_adventureStateSubscription?.Dispose();
		_adventureStateSubscription = null;
		base._ExitTree();
	}

	private void UpdateSettings(string displayName)
	{
		try
		{
			Game.UserSettings.Update(_ => ReadSettingsFromControls());
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Failed to apply setting '{displayName}'.", exception);
			ApplySettingsToControls(Game.UserSettings.Current);
			ApplyNoRegretRestrictions();
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void OnBattleSpeedMultiplierChanged(double value)
	{
		var multiplier = ClampBattleSpeedMultiplier((int)Math.Round(value));
		_battleSpeedMultiplierSlider.SetValueNoSignal(multiplier);
		UpdateBattleSpeedMultiplierLabel(multiplier);
		UpdateSettings("战斗加速倍率");
	}

	private void ApplySettingsToControls(UserSettingsRecord settings)
	{
		_showBattleBoardCheckBox.SetPressedNoSignal(settings.ShowBattleBoard);
		_showBattleHpCheckBox.SetPressedNoSignal(settings.ShowBattleHp);
		_autoSaveCheckBox.SetPressedNoSignal(settings.AutoSave);
		_largeMapMovementAnimationCheckBox.SetPressedNoSignal(settings.LargeMapMovementAnimationEnabled);
		_autoBattleCheckBox.SetPressedNoSignal(settings.AutoBattle);
		_hideBattleHudDuringAutomatedTurnsCheckBox.SetPressedNoSignal(settings.HideBattleHudDuringAutomatedTurns);
		_battleSpeedUpCheckBox.SetPressedNoSignal(settings.BattleSpeedUp);
		_typewriterDialogCheckBox.SetPressedNoSignal(settings.DialogueTypewriterEnabled);
		_battleSpeedMultiplierSlider.SetValueNoSignal(ClampBattleSpeedMultiplier(settings.BattleSpeedMultiplier));
		UpdateBattleSpeedMultiplierLabel((int)_battleSpeedMultiplierSlider.Value);
		OptionButtonBinder.SelectEnumNoSignal(_screenAspectOptionButton, settings.ScreenAspectMode);
		OptionButtonBinder.SelectEnumNoSignal(_windowDisplayOptionButton, settings.WindowDisplayMode);
		_musicCheckBox.SetPressedNoSignal(settings.MusicEnabled);
		_sfxCheckBox.SetPressedNoSignal(settings.SfxEnabled);
	}

	private UserSettingsRecord ReadSettingsFromControls()
	{
		var current = Game.UserSettings.Current;
		return current with
		{
			ShowBattleHp = _showBattleHpCheckBox.ButtonPressed,
			AutoSave = Game.State.Adventure.NoRegret ? current.AutoSave : _autoSaveCheckBox.ButtonPressed,
			AutoBattle = _autoBattleCheckBox.ButtonPressed,
			HideBattleHudDuringAutomatedTurns = _hideBattleHudDuringAutomatedTurnsCheckBox.ButtonPressed,
			BattleSpeedUp = _battleSpeedUpCheckBox.ButtonPressed,
			BattleSpeedMultiplier = ClampBattleSpeedMultiplier((int)Math.Round(_battleSpeedMultiplierSlider.Value)),
			MusicEnabled = _musicCheckBox.ButtonPressed,
			SfxEnabled = _sfxCheckBox.ButtonPressed,
			DialogueTypewriterEnabled = _typewriterDialogCheckBox.ButtonPressed,
			ShowBattleBoard = _showBattleBoardCheckBox.ButtonPressed,
			LargeMapMovementAnimationEnabled = _largeMapMovementAnimationCheckBox.ButtonPressed,
			ScreenAspectMode = OptionButtonBinder.ReadSelectedEnum(_screenAspectOptionButton, ScreenAspectMode.Unlimited),
			WindowDisplayMode = OptionButtonBinder.ReadSelectedEnum(_windowDisplayOptionButton, WindowDisplayMode.Windowed),
		};
	}

	private void ApplyNoRegretRestrictions()
	{
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
		{
			return;
		}

		var noRegret = Game.State.Adventure.NoRegret;
		_autoSaveCheckBox.Disabled = noRegret;
		_autoSaveCheckBox.SetPressedNoSignal(noRegret || Game.UserSettings.Current.AutoSave);
		_autoSaveCheckBox.TooltipText = noRegret ? "无悔周目强制开启自动存档。" : string.Empty;
	}

	private void UpdateBattleSpeedMultiplierLabel(int multiplier) =>
		_battleSpeedMultiplierValueLabel.Text = $"{multiplier}倍";

	private static int ClampBattleSpeedMultiplier(int multiplier) =>
		Math.Clamp(multiplier, MinBattleSpeedMultiplier, MaxBattleSpeedMultiplier);
}
