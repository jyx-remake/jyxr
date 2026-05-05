using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Godot.UI.Battle;

public sealed class BattlePresenter
{
	private const int PlayerTeam = 1;

	public BattleHeaderView CreateHeader(BattleState state, BattleUiMode mode)
	{
		ArgumentNullException.ThrowIfNull(state);
		var actingUnit = TryGetActingUnit(state);
		var title = actingUnit is null
			? "战场"
			: $"行动：{actingUnit.Character.Name}";
		var subtitle = mode switch
		{
			BattleUiMode.WaitingTimeline => "等待行动点推进",
			BattleUiMode.UnitActing => "选择行动",
			BattleUiMode.SelectingMove => "选择移动目标",
			BattleUiMode.SelectingSkill => "选择技能",
			BattleUiMode.SelectingSkillTarget => "选择技能目标",
			BattleUiMode.SelectingItem => "选择物品",
			BattleUiMode.SelectingItemTarget => "选择物品目标",
			BattleUiMode.BattleEnded => "战斗结束",
			_ => string.Empty,
		};
		return new BattleHeaderView(title, subtitle);
	}

	public IReadOnlyList<BattleCellView> CreateCells(
		BattleState state)
	{
		ArgumentNullException.ThrowIfNull(state);

		var actingUnitId = state.CurrentAction?.ActingUnitId;
		var cells = new List<BattleCellView>(state.Grid.Width * state.Grid.Height);
		for (var y = 0; y < state.Grid.Height; y++)
		{
			for (var x = 0; x < state.Grid.Width; x++)
			{
				var position = new GridPosition(x, y);
				var unit = state.GetUnitAt(position);
				cells.Add(new BattleCellView(
					position,
					string.Empty,
					unit is not null,
					unit?.Team == PlayerTeam,
					unit is not null && !unit.IsAlive,
					unit is not null && string.Equals(unit.Id, actingUnitId, StringComparison.Ordinal)));
			}
		}

		return cells;
	}

	public BattleUnitPanelView CreateUnitPanel(BattleState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var unit = TryGetActingUnit(state);
		if (unit is null)
		{
			return new BattleUnitPanelView("无行动单位", string.Empty);
		}

		var detail =
			$"HP {unit.Hp}/{unit.MaxHp}\n" +
			$"MP {unit.Mp}/{unit.MaxMp}\n" +
			$"怒气 {unit.Rage}\n" +
			$"行动 {(int)Math.Round(unit.ActionGauge, MidpointRounding.AwayFromZero)}\n" +
			$"集气 {unit.ActionSpeed:0.##}\n" +
			$"移动 {unit.MovePower}";
		return new BattleUnitPanelView(unit.Character.Name, detail);
	}

	public IReadOnlyList<BattleSkillOptionView> CreateSkillList(BattleUnit unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		return CollectUsableSkills(unit)
			.Select(skill => new BattleSkillOptionView(
				skill,
				$"{skill.Name}  MP {skill.MpCost}  怒 {skill.RageCost}"))
			.ToList();
	}

	public IReadOnlyList<BattleItemView> CreateItemList(Inventory inventory)
	{
		ArgumentNullException.ThrowIfNull(inventory);
		return inventory.Entries
			.Where(static entry =>
				entry.Definition.Type == ItemType.Consumable &&
				entry.Definition.UseEffects.Count > 0)
			.Select(static entry => new BattleItemView(entry, FormatInventoryEntry(entry)))
			.ToList();
	}

	public static BattleUnit? TryGetActingUnit(BattleState state) =>
		state.CurrentAction is { } context
			? state.TryGetUnit(context.ActingUnitId)
			: null;

	private static IEnumerable<SkillInstance> CollectUsableSkills(BattleUnit unit) =>
		unit.Character.GetExternalSkills()
			.Where(static skill => skill.IsActive)
			.Cast<SkillInstance>()
			.Concat(unit.Character.GetSpecialSkills().Where(static skill => skill.IsActive));

	private static string FormatInventoryEntry(InventoryEntry entry) =>
		entry switch
		{
			StackInventoryEntry stack => $"{stack.Definition.Name} x{stack.Quantity}",
			EquipmentInstanceInventoryEntry equipment => equipment.Equipment.Definition.Name,
			_ => entry.Definition.Name,
		};
}

public sealed record BattleHeaderView(string Title, string Subtitle);

public sealed record BattleCellView(
	GridPosition Position,
	string Label,
	bool HasUnit,
	bool IsPlayerUnit,
	bool IsDead,
	bool IsActing);

public sealed record BattleUnitPanelView(string Title, string Detail);

public sealed record BattleSkillOptionView(SkillInstance Skill, string Label);

public sealed record BattleItemView(InventoryEntry Entry, string Label);
