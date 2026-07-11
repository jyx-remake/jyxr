using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI.Battle;

internal sealed class BattleSettingsController(
    BattleBoardView board,
    CanvasItem speedUpActive,
    CanvasItem autoBattleActive,
    Func<BattleFlowOrchestrator?> orchestratorProvider,
    Func<bool> isInsideTree,
    Func<bool> isResolvingPresentation,
    Action refreshActions,
    Action<string> appendLog)
{
    private const int MinSpeedMultiplier = 1;
    private const int MaxSpeedMultiplier = 5;
    private readonly LocalUserSettingsStore _store = new();
    private readonly double _initialTimeScale = Engine.TimeScale;
    private bool _speedUpEnabled;
    private int _speedMultiplier = 2;

    public void Load()
    {
        var settings = _store.LoadOrDefault();
        _speedUpEnabled = settings.BattleSpeedUp;
        _speedMultiplier = Math.Clamp(settings.BattleSpeedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
        board.ShowBaseBoard = settings.ShowBattleBoard;
        orchestratorProvider()?.SetAutoBattleEnabled(settings.AutoBattle);
        ApplyTimeScale();
        RefreshButtons();
    }

    public void ToggleSpeedUp()
    {
        _speedUpEnabled = !_speedUpEnabled;
        ApplyTimeScale();
        Save();
        RefreshButtons();
        appendLog(_speedUpEnabled ? "已开启战斗加速。" : "已关闭战斗加速。");
    }

    public Task ToggleAutoBattleAsync()
    {
        var orchestrator = orchestratorProvider();
        if (orchestrator is null) return Task.CompletedTask;

        var enabled = !orchestrator.IsAutoBattleEnabled;
        orchestrator.SetAutoBattleEnabled(enabled);
        Save();
        appendLog(enabled ? "已开启自动战斗。" : "已关闭自动战斗。");
        if (isInsideTree())
        {
            refreshActions();
            RefreshButtons();
        }
        if (enabled && !isResolvingPresentation()) _ = orchestrator.ContinueBattleFlowAsync();
        return Task.CompletedTask;
    }

    public void RefreshButtons()
    {
        if (!isInsideTree()) return;
        speedUpActive.Visible = _speedUpEnabled;
        autoBattleActive.Visible = orchestratorProvider()?.IsAutoBattleEnabled == true;
    }

    public void RestoreTimeScale() => Engine.TimeScale = _initialTimeScale;

    private void ApplyTimeScale() =>
        Engine.TimeScale = _speedUpEnabled ? _initialTimeScale * _speedMultiplier : _initialTimeScale;

    private void Save()
    {
        var settings = _store.LoadOrDefault();
        _store.Save(settings with
        {
            AutoBattle = orchestratorProvider()?.IsAutoBattleEnabled == true,
            BattleSpeedUp = _speedUpEnabled,
            BattleSpeedMultiplier = _speedMultiplier,
        });
    }
}
