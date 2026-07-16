using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Presentation.Battle;

public interface IBattleFlowContext
{
	BattleState State { get; }
	BattleEngine Engine { get; }
	bool IsAutoBattleEnabled { get; }
	bool IsSurrenderRequested { get; }
	int PlayerTeam { get; }
	BattleUnit? ActingUnit { get; }
	void RenderInteraction(BattleInteractionState interaction);
	void CommitBattleStateToView(BattleInteractionState interaction);
	void BeginActionPresentation(BattleInteractionState interaction);
	void ToggleAutoBattle();
	void RequestSurrender();
	BattleUnit AdvanceTimelineToNextAction();
	BattleTurnPlan DecideAutomatedTurn(BattleUnit actingUnit);
	void AppendTurnLog(BattleUnit unit, bool isAutomated);
	Task<bool> ExecuteMoveAsync(BattleUnit actingUnit, GridPosition destination);
	Task<bool> ExecuteSkillAsync(BattleUnit actingUnit, SkillInstance skill, GridPosition target);
	bool ExecuteItem(BattleUnit actingUnit, InventoryEntry item, string targetUnitId);
	bool ExecuteRest(BattleUnit actingUnit);
	bool ExecuteEndAction(BattleUnit actingUnit);
	Task<bool> RollbackMoveAsync(BattleUnit actingUnit);
	SkillInstance? ResolveDefaultSkill(BattleUnit actingUnit);
	SkillInstance? ResolveSkill(BattleUnit actingUnit, string? skillId);
	Task<InventoryEntry?> ShowItemPanelAsync();
	void ShowStatusPanel();
	bool TryGetBattleResult(out bool isWin);
	Task CompleteBattleAsync(bool isWin);
}
