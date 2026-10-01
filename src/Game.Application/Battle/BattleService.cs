using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application;

public sealed class BattleService
{
    private readonly GameSession _session;
    private readonly BattleStateFactory _stateFactory;
    private readonly BattleSettlementService _settlementService;
    private readonly BattleCarryoverService _carryoverService;

    public BattleService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        var zhenlongqijuFactory = new ZhenlongqijuBattleFactory(session);
        var characterFactory = new ProceduralBattleCharacterFactory(session);
        _stateFactory = new BattleStateFactory(session, characterFactory, zhenlongqijuFactory);
        _settlementService = new BattleSettlementService(session, zhenlongqijuFactory);
        _carryoverService = new BattleCarryoverService(session);
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

    public void RestorePartyBattleResources() =>
        _carryoverService.RestorePartyBattleResources();
}
