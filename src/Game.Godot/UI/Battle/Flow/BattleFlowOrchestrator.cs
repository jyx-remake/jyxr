using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleFlowOrchestrator
{
    private const int PlayerTeam = 1;

    private readonly BattleScreen _screen;
    private readonly BattleEngine _engine;
    private readonly IBattleAgent _battleAgent;
    private bool _autoBattleEnabled;
    private bool _isContinuingFlow;
    private bool? _forcedBattleResult;

    public BattleFlowOrchestrator(BattleScreen screen, BattleState state)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        State = state ?? throw new ArgumentNullException(nameof(state));
        _engine = new BattleEngine(
            buffResolver: buffId => GameRoot.ContentRepository.GetBuff(buffId),
            legendSkillsProvider: () => GameRoot.ContentRepository.GetLegendSkills());
        _battleAgent = new BasicEnemyBattleAgent(new BattleTurnCandidateGenerator(_engine));
        _screen.BindState(State);
    }

    public BattleState State { get; }

    internal BattleEngine Engine => _engine;

    public bool IsAutoBattleEnabled => _autoBattleEnabled;

    public IReadOnlyDictionary<GridPosition, int> GetReachablePositions()
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null)
        {
            return new Dictionary<GridPosition, int>();
        }

        return _engine.GetReachablePositions(State, actingUnit.Id);
    }

    public async Task StartAsync()
    {
        await ContinueBattleFlowAsync();
    }

    public void Surrender()
    {
        _autoBattleEnabled = false;
        _forcedBattleResult = false;
        _screen.AppendLog("我方选择投降。");
        _screen.ShowBattleEnded(isWin: false);
    }

    public void SetAutoBattleEnabled(bool enabled)
    {
        _autoBattleEnabled = enabled;
    }

    public Task TryRollbackMoveAsync()
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null)
        {
            return Task.CompletedTask;
        }

        var eventStart = State.Events.Count;
        var rollbackResult = _engine.RollbackMove(State, actingUnit.Id);
        _screen.AppendResult(rollbackResult, eventStart);
        _screen.RefreshAll();
        return Task.CompletedTask;
    }

    public async Task TryMoveAsync(GridPosition destination)
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null || actingUnit.Team != PlayerTeam)
        {
            return;
        }

        var eventStart = State.Events.Count;
        var result = _engine.MoveTo(State, actingUnit.Id, destination);
        var movementPath = result.Success && State.CurrentAction is not null
            ? State.CurrentAction.MovementTrace.ToArray()
            : Array.Empty<GridPosition>();
        _screen.AppendResult(result, eventStart);
        if (!result.Success)
        {
            _screen.RefreshAll();
            return;
        }

        await _screen.PlayMoveAsync(actingUnit, movementPath);
        await HandlePlayerPostMoveAsync(actingUnit);
    }

    public async Task TryCastSkillAsync(SkillInstance skill, GridPosition target)
    {
        ArgumentNullException.ThrowIfNull(skill);
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null || actingUnit.Team != PlayerTeam)
        {
            return;
        }

        var eventStart = State.Events.Count;
        var result = _engine.CastSkill(State, actingUnit.Id, skill, target);
        if (!result.Success)
        {
            _screen.AppendResult(result, eventStart);
            _screen.RefreshAll();
            return;
        }

        await _screen.PlaySkillAsync(actingUnit, skill, result, eventStart);
        await ContinueAfterResolvedPlayerActionAsync();
    }

    public async Task TryUseItemAsync(InventoryEntry item, string targetUnitId)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUnitId);
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null || actingUnit.Team != PlayerTeam)
        {
            return;
        }

        var eventStart = State.Events.Count;
        var result = _engine.UseItem(State, actingUnit.Id, item.Definition, targetUnitId);
        if (result.Success)
        {
            _screen.ApplyActingUnitFacing(actingUnit);
        }

        _screen.AppendResult(result, eventStart);
        if (!result.Success)
        {
            _screen.RefreshAll();
            return;
        }

        GameRoot.InventoryService.RemoveItem(item.Definition);
        await ContinueAfterResolvedPlayerActionAsync();
    }

    public async Task TryRestAsync()
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null || actingUnit.Team != PlayerTeam)
        {
            return;
        }

        var eventStart = State.Events.Count;
        var result = _engine.Rest(State, actingUnit.Id);
        _screen.AppendResult(result, eventStart);
        if (!result.Success)
        {
            _screen.RefreshAll();
            return;
        }

        await ContinueAfterResolvedPlayerActionAsync();
    }

    public async Task TryEndActionAsync()
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(State);
        if (actingUnit is null || actingUnit.Team != PlayerTeam)
        {
            return;
        }

        var eventStart = State.Events.Count;
        var result = _engine.EndAction(State, actingUnit.Id);
        _screen.AppendResult(result, eventStart);
        if (!result.Success)
        {
            _screen.RefreshAll();
            return;
        }

        await ContinueAfterResolvedPlayerActionAsync();
    }

    public async Task ContinueBattleFlowAsync()
    {
        if (_isContinuingFlow)
        {
            return;
        }

        _isContinuingFlow = true;
        try
        {
            while (true)
            {
                if (TryCompleteBattle())
                {
                    return;
                }

                var actingUnit = State.CurrentAction is null
                    ? AdvanceTimelineToNextAction()
                    : BattlePresenter.TryGetActingUnit(State) ??
                        throw new InvalidOperationException("Current action does not have a valid acting unit.");

                if (actingUnit.Team == PlayerTeam && !_autoBattleEnabled)
                {
                    _screen.ShowPlayerTurn(actingUnit);
                    return;
                }

                _screen.ShowWaitingTimeline();
                _screen.AppendLog(actingUnit.Team == PlayerTeam
                    ? $"轮到 {actingUnit.Character.Name} 自动行动。"
                    : $"轮到 {actingUnit.Character.Name} 行动。");
                await ExecuteAutomatedTurnAsync(actingUnit);
            }
        }
        finally
        {
            _isContinuingFlow = false;
        }
    }

    private BattleUnit AdvanceTimelineToNextAction()
    {
        _screen.ShowWaitingTimeline();
        return _engine.AdvanceUntilNextAction(State);
    }

    private async Task HandlePlayerPostMoveAsync(BattleUnit actingUnit)
    {
        if (TryCompleteBattle())
        {
            return;
        }

        if (_autoBattleEnabled)
        {
            await ContinueBattleFlowAsync();
            return;
        }

        _screen.ShowPlayerPostMove(actingUnit);
    }

    private async Task ContinueAfterResolvedPlayerActionAsync()
    {
        if (TryCompleteBattle())
        {
            return;
        }

        await ContinueBattleFlowAsync();
    }

    private async Task ExecuteAutomatedTurnAsync(BattleUnit actingUnit)
    {
        var plan = _battleAgent.Decide(State, actingUnit.Id);
        if (plan.MoveDestination != actingUnit.Position)
        {
            var moveEventStart = State.Events.Count;
            var moveResult = _engine.MoveTo(State, actingUnit.Id, plan.MoveDestination);
            var movementPath = moveResult.Success && State.CurrentAction is not null
                ? State.CurrentAction.MovementTrace.ToArray()
                : Array.Empty<GridPosition>();
            _screen.AppendResult(moveResult, moveEventStart);
            if (moveResult.Success)
            {
                await _screen.PlayMoveAsync(actingUnit, movementPath);
                if (TryCompleteBattle())
                {
                    return;
                }
            }
        }

        if (plan.MainAction.Kind == BattleMainActionKind.CastSkill &&
            plan.MainAction.TargetPosition is { } targetPosition &&
            TryResolveSkill(actingUnit, plan.MainAction.SkillId) is { } skill)
        {
            var skillEventStart = State.Events.Count;
            var result = _engine.CastSkill(State, actingUnit.Id, skill, targetPosition);
            if (result.Success)
            {
                await _screen.PlaySkillAsync(actingUnit, skill, result, skillEventStart);
                return;
            }

            _screen.AppendResult(result, skillEventStart);
        }

        var restEventStart = State.Events.Count;
        _screen.AppendResult(_engine.Rest(State, actingUnit.Id), restEventStart);
    }

    private bool TryCompleteBattle()
    {
        if (_forcedBattleResult is { } forcedBattleResult)
        {
            _autoBattleEnabled = false;
            _screen.ShowBattleEnded(forcedBattleResult);
            return true;
        }

        var playerAlive = State.Units.Any(static unit => unit.Team == PlayerTeam && unit.IsAlive);
        var enemyAlive = State.Units.Any(static unit => unit.Team != PlayerTeam && unit.IsAlive);
        if (playerAlive && enemyAlive)
        {
            return false;
        }

        _autoBattleEnabled = false;
        _screen.ShowBattleEnded(playerAlive);
        return true;
    }

    private static SkillInstance? TryResolveSkill(BattleUnit actingUnit, string? skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        return BattleSkillCatalog.CollectSelectableSkills(actingUnit)
            .FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal));
    }
}
