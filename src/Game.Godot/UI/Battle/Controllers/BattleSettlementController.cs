using Game.Application;
using Game.Core.Battle;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleSettlementController(
    BattleScreen owner,
    PackedScene settlementPanelScene,
    Control overlayRoot,
    BattlePresenter presenter,
    Action<string> appendLog)
{
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isEnding;

    public async Task<bool> AwaitAsync(CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(() => Cancel(cancellationToken))
            : default;
        return await _completion.Task;
    }

    public async Task CompleteAsync(bool isWin, BattleState? state, SpecialBattleRequest? request)
    {
        if (_isEnding) return;
        _isEnding = true;
        try
        {
            appendLog(isWin ? "战斗胜利。" : "战斗失败。");

            OrdinaryBattleVictorySettlement? settlement = null;
            if (state is not null && request is not null)
            {
                GameRoot.BattleService.ApplyPlayerBattleCarryover(state);
                if (isWin)
                {
                    settlement = GameRoot.BattleService.PreviewVictorySettlement(state, request);
                    GameRoot.BattleService.ApplyOrdinaryVictorySettlement(state, settlement);
                }
            }

            await ShowPanelAsync(isWin, settlement);
            _completion.TrySetResult(isWin);
        }
        catch (Exception exception)
        {
            _completion.TrySetException(exception);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(owner)) owner.QueueFree();
        }
    }

    public void Cancel()
    {
        if (!_completion.Task.IsCompleted) _completion.TrySetCanceled();
    }

    private void Cancel(CancellationToken cancellationToken)
    {
        if (_completion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(owner)) owner.QueueFree();
    }

    private async Task ShowPanelAsync(bool isWin, OrdinaryBattleVictorySettlement? settlement)
    {
        if (settlementPanelScene.Instantiate() is not BattleSettlementPanel panel)
            throw new InvalidOperationException("Battle settlement panel scene root must be BattleSettlementPanel.");
        panel.Configure(presenter.CreateSettlementView(isWin, settlement));
        overlayRoot.AddChild(panel);
        await panel.AwaitConfirmationAsync();
    }
}
