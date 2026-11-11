using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;
using System.Runtime.CompilerServices;

namespace Game.Application;

public sealed class BattleService
{
    private readonly GameSession _session;
    private readonly BattleStateFactory _stateFactory;
    private readonly BattleSettlementService _settlementService;
    private readonly BattleCarryoverService _carryoverService;
    public IRuntimeHost Host { get; }
    
    private readonly BattleEventControl _battleeventcontrol;
    private readonly BattleRuntime _runtime = new();
    public BattleService(GameSession session, IRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        var zhenlongqijuFactory = new ZhenlongqijuBattleFactory(session);
        var characterFactory = new ProceduralBattleCharacterFactory(session);
        _stateFactory = new BattleStateFactory(session, characterFactory, zhenlongqijuFactory);
        _settlementService = new BattleSettlementService(session, zhenlongqijuFactory);
        _carryoverService = new BattleCarryoverService(session);
        Host = host ?? throw new ArgumentNullException(nameof(host));
        
        _battleeventcontrol = new BattleEventControl(session, _runtime, Host);
    }

    public BattleState BuildBattleState(SpecialBattleRequest request) =>
        _stateFactory.BuildBattleState(request);
    public IReadOnlyList<BattleJoinCombatant> SpawnCombatant(BattleUnit actingUnit, BattleState state, IReadOnlyList<string> characterIds, IReadOnlyList<GridPosition> impactedPositions)
    {
        return _stateFactory.SpawnCombatant(actingUnit, state, characterIds, impactedPositions);
    }
    public OrdinaryBattleVictorySettlement PreviewVictorySettlement(
        BattleState state,
        SpecialBattleRequest request) =>
        _settlementService.PreviewVictorySettlement(state, request);

    public void ApplyOrdinaryVictorySettlement(
        BattleState state,
        OrdinaryBattleVictorySettlement settlement) =>
        _settlementService.ApplyVictorySettlement(state, settlement);

    public void ApplyPlayerBattleCarryover(BattleState state) =>
        _carryoverService.ApplyPlayerBattleCarryover(state);

    public void RecordDefeatedEnemies(BattleState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var defeatedEnemyCount = state.Units.Count(unit =>
            unit.Team != _session.Config.BattlePlayerTeam &&
            !unit.IsAlive);
        if (defeatedEnemyCount > 0)
        {
            _session.ProfileService.AddKills(defeatedEnemyCount);
        }
    }
    private void ApplyHpChange(BattleUnit unit, double amount, string kind)
    => _stateFactory.ApplyHpChange(unit,amount,kind);

public void UnitsMpChange(BattleState state, IReadOnlyList<string> unitIds, IReadOnlyList<double> amounts, IReadOnlyList<string>? kinds = null)
=> _stateFactory.UnitsMpChange(state, unitIds, amounts, kinds);

    public void RestorePartyBattleResources() =>
        _carryoverService.RestorePartyBattleResources();



    public void TryGetSegment() { 
    
    }
    public BattleEventSegmentEntry FindBattleEventScript(string battleEventId) =>
        _session.ContentRepository.GetBattleEventSegment(battleEventId);

    public async Task<bool> RunBattleEventAsync(
        BattleMainSegment battleEventSegment,
        BattleState state,
        StoryExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleEventSegment.Name);
        var executionState = _session.State;
        var entry = FindBattleEventScript(battleEventSegment.Name);

        var completed = false;
        try
        {
            await foreach (var battleEvent in _battleeventcontrol.RunAsync(entry, battleEventSegment, state, context, cancellationToken))
            {
                completed = battleEvent is Core.Battle.SegmentCompletedEvent;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _session.DiagnosticLogger.Error($"Battle event '{battleEventSegment.Name}' failed.", exception);
            throw;
        }

        return completed && ReferenceEquals(executionState, _session.State);
    }

}
