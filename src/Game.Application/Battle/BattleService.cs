using Game.Core.Battle;
using Game.Core.Model;

namespace Game.Application;

public sealed class BattleService
{
	private readonly BattleStateFactory _stateFactory;
	private readonly BattleSettlementService _settlementService;
	private readonly BattleCarryoverService _carryoverService;

	public BattleService(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		var zhenlongqijuFactory = new ZhenlongqijuBattleFactory(session);
		var characterFactory = new ProceduralBattleCharacterFactory(session);
		_stateFactory = new BattleStateFactory(session, characterFactory, zhenlongqijuFactory);
		_settlementService = new BattleSettlementService(session, zhenlongqijuFactory);
		_carryoverService = new BattleCarryoverService(session);
	}

	public BattleState BuildBattleState(SpecialBattleRequest request) =>
		_stateFactory.BuildBattleState(request);


	public void SpawnCombatant(BattleUnit actingUnit, BattleState state, List<string> characterIds, IReadOnlyList<GridPosition> ImpactedPositions)
	{
		_stateFactory.SpawnCombatant( actingUnit,  state,characterIds,ImpactedPositions);
	 }

	public IBattleStateFactory stateFactory => _stateFactory;

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

	public void RestorePartyBattleResources() =>
		_carryoverService.RestorePartyBattleResources();
}
