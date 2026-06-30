using Game.Core.Battle;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleStatusUnitButton : Button
{
	private static readonly Color PlayerColor = new(1f, 1f, 0f, 1f);
	private static readonly Color EnemyColor = Colors.Red;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _hpLabel = null!;
	private Control _selectedFrame = null!;
	private BattleUnit? _unit;
	private bool _isPlayerTeam;
	private bool _isSelected;

	[Signal]
	public delegate void UnitSelectedEventHandler(string unitId);

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_hpLabel = GetNode<Label>("%HpLabel");
		_selectedFrame = GetNode<Control>("%SelectedFrame");
		Pressed += OnPressed;
		RefreshView();
	}

	public void Setup(BattleUnit unit, bool isPlayerTeam, bool isSelected)
	{
		ArgumentNullException.ThrowIfNull(unit);
		_unit = unit;
		_isPlayerTeam = isPlayerTeam;
		_isSelected = isSelected;
		RefreshView();
	}

	private void RefreshView()
	{
		if (_unit is null || !IsInsideTree())
		{
			return;
		}

		TooltipText = _unit.Character.Name;
		_nameLabel.Text = _unit.Character.Name;
		_nameLabel.AddThemeColorOverride("font_color", ResolveTeamColor());
		_hpLabel.Text = $"{_unit.Hp}/{_unit.MaxHp}";
		_selectedFrame.Visible = _isSelected;
		Modulate = _unit.IsAlive ? Colors.White : new Color(0.55f, 0.55f, 0.55f, 0.85f);

		var portrait = AssetResolver.LoadCharacterPortrait(_unit.Character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}
	}

	private Color ResolveTeamColor() => _isPlayerTeam ? PlayerColor : EnemyColor;

	private void OnPressed()
	{
		if (_unit is null)
		{
			return;
		}

		EmitSignal(SignalName.UnitSelected, _unit.Id);
	}
}
