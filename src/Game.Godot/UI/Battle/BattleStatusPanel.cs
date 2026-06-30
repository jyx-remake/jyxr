using Game.Core.Battle;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleStatusPanel : Control
{
	[Export]
	public PackedScene UnitButtonScene { get; set; } = null!;

	private CharacterPanel _characterPanel = null!;
	private VBoxContainer _unitList = null!;
	private BattleState? _state;
	private string? _selectedUnitId;
	private int _playerTeam;

	public override void _Ready()
	{
		_characterPanel = GetNode<CharacterPanel>("%CharacterPanel");
		_unitList = GetNode<VBoxContainer>("%UnitList");
		_characterPanel.ClosePanelRequested += QueueFree;
		Refresh();
	}

	public void Configure(BattleState state, int playerTeam)
	{
		ArgumentNullException.ThrowIfNull(state);
		_state = state;
		_playerTeam = playerTeam;
		_selectedUnitId = SelectInitialUnit(state, playerTeam);
		if (IsInsideTree())
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _state is null)
		{
			return;
		}

		var units = EnumerateUnits(_state, _playerTeam).ToArray();
		if (units.Length == 0)
		{
			return;
		}

		var selectedUnit = units.FirstOrDefault(unit =>
			string.Equals(unit.Id, _selectedUnitId, StringComparison.Ordinal)) ?? units[0];
		_selectedUnitId = selectedUnit.Id;
		_characterPanel.Configure(selectedUnit.Character, CharacterPanelMode.ReadOnly, selectedUnit);
		RefreshUnitList(units);
	}

	private void RefreshUnitList(IReadOnlyList<BattleUnit> units)
	{
		ClearChildren(_unitList);
		foreach (var unit in units)
		{
			_unitList.AddChild(CreateUnitButton(unit));
		}
	}

	private BattleStatusUnitButton CreateUnitButton(BattleUnit unit)
	{
		if (UnitButtonScene is null)
		{
			throw new InvalidOperationException("BattleStatusPanel.UnitButtonScene is not assigned.");
		}

		var instance = UnitButtonScene.Instantiate();
		if (instance is not BattleStatusUnitButton button)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle status unit button scene root must be BattleStatusUnitButton.");
		}

		button.Setup(unit, unit.Team == _playerTeam, string.Equals(unit.Id, _selectedUnitId, StringComparison.Ordinal));
		button.UnitSelected += OnUnitSelected;
		return button;
	}

	private void OnUnitSelected(string unitId)
	{
		_selectedUnitId = unitId;
		Refresh();
	}

	private static string SelectInitialUnit(BattleState state, int playerTeam) =>
		EnumerateUnits(state, playerTeam).First().Id;

	private static IEnumerable<BattleUnit> EnumerateUnits(BattleState state, int playerTeam) =>
		state.Units
			.OrderBy(unit => unit.Team == playerTeam ? 0 : 1)
			.ThenBy(unit => unit.IsAlive ? 0 : 1)
			.ThenBy(unit => unit.Team)
			.ThenBy(unit => unit.Id, StringComparer.Ordinal);

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			child.QueueFree();
		}
	}
}
