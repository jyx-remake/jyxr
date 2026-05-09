using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Godot.UI.Battle;

public enum BattleUiMode
{
	WaitingTimeline,
	UnitActing,
	SelectingMove,
	SelectingSkill,
	SelectingSkillTarget,
	SelectingItem,
	SelectingItemTarget,
	BattleEnded,
}

public sealed class BattleUiStateMachine
{
	public BattleUiMode Mode { get; private set; } = BattleUiMode.WaitingTimeline;

	public SkillInstance? SelectedSkill { get; private set; }

	public InventoryEntry? SelectedItem { get; private set; }

	public bool IsBattleEnded => Mode == BattleUiMode.BattleEnded;

	public void WaitTimeline() => SetMode(BattleUiMode.WaitingTimeline);

	public void ActUnit() => SetMode(BattleUiMode.UnitActing);

	public void SelectMove() => SetMode(BattleUiMode.SelectingMove);

	public void SelectSkillList() => SetMode(BattleUiMode.SelectingSkill);

	public void SelectSkillTarget(SkillInstance skill)
	{
		ArgumentNullException.ThrowIfNull(skill);
		SelectedSkill = skill;
		SelectedItem = null;
		Mode = BattleUiMode.SelectingSkillTarget;
	}

	public void SelectItemList() => SetMode(BattleUiMode.SelectingItem);

	public void SelectItemTarget(InventoryEntry item)
	{
		ArgumentNullException.ThrowIfNull(item);
		SelectedItem = item;
		SelectedSkill = null;
		Mode = BattleUiMode.SelectingItemTarget;
	}

	public void EndBattle() => SetMode(BattleUiMode.BattleEnded);

	private void SetMode(BattleUiMode mode)
	{
		Mode = mode;
		if (mode != BattleUiMode.SelectingSkillTarget)
		{
			SelectedSkill = null;
		}

		if (mode != BattleUiMode.SelectingItemTarget)
		{
			SelectedItem = null;
		}
	}
}
