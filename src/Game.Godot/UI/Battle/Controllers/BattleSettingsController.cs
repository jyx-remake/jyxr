using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI.Battle;

internal sealed class BattleSettingsController(
    BattleBoardView board,
    CanvasItem speedUpActive,
    CanvasItem autoBattleActive,
    Func<bool> isAutoBattleEnabled,
    Action<bool> setAutoBattleEnabled,
    Func<bool> isInsideTree,
    Action<string> appendLog)
{
    private const int MinSpeedMultiplier = 1;
    private const int MaxSpeedMultiplier = 5;
    private readonly LocalUserSettingsStore _store = new();
    private readonly double _initialTimeScale = Engine.TimeScale;
    private UserSettingsRecord? _loadedSettings;
    private bool _speedUpEnabled;
    private int _speedMultiplier = 2;

    public void Load()
    {
        var settings = _store.LoadOrDefault();
        _loadedSettings = settings;
        _speedUpEnabled = settings.BattleSpeedUp;
        _speedMultiplier = Math.Clamp(settings.BattleSpeedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
        board.ShowBaseBoard = settings.ShowBattleBoard;
        setAutoBattleEnabled(settings.AutoBattle);
        ApplyTimeScale();
        RefreshButtons();
    }

    public void ToggleSpeedUp()
    {
        _speedUpEnabled = !_speedUpEnabled;
        ApplyTimeScale();
        RefreshButtons();
        appendLog(_speedUpEnabled ? "已开启战斗加速。" : "已关闭战斗加速。");
    }

    public void PresentAutoBattle(bool enabled)
    {
        appendLog(enabled ? "已开启自动战斗。" : "已关闭自动战斗。");
        if (isInsideTree())
        {
            RefreshButtons();
        }
    }

    public void RefreshButtons()
    {
        if (!isInsideTree()) return;
        speedUpActive.Visible = _speedUpEnabled;
        autoBattleActive.Visible = isAutoBattleEnabled();
    }

    public void RestoreTimeScale() => Engine.TimeScale = _initialTimeScale;

    public void Save()
    {
        var loadedSettings = _loadedSettings ??
            throw new InvalidOperationException("Battle settings must be loaded before they can be saved.");
        var settings = loadedSettings with
        {
            AutoBattle = isAutoBattleEnabled(),
            BattleSpeedUp = _speedUpEnabled,
            BattleSpeedMultiplier = _speedMultiplier,
        };

        if (settings == loadedSettings)
        {
            return;
        }

        _store.Save(settings);
    }

    private void ApplyTimeScale() =>
        Engine.TimeScale = _speedUpEnabled ? _initialTimeScale * _speedMultiplier : _initialTimeScale;
}
