using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterAttributeTab : Control
{
	private static readonly (StatType StatType, string NodeName)[] DisplayedStats =
	[
		(StatType.Quanzhang, "QuanzhangLabel"),
		(StatType.Jianfa, "JianfaLabel"),
		(StatType.Daofa, "DaofaLabel"),
		(StatType.Qimen, "QimenLabel"),
		(StatType.Bili, "BiliLabel"),
		(StatType.Shenfa, "ShenfaLabel"),
		(StatType.Wuxing, "WuxingLabel"),
		(StatType.Fuyuan, "FuyuanLabel"),
		(StatType.Gengu, "GenguLabel"),
		(StatType.Dingli, "DingliLabel"),
	];

	private static readonly (string ButtonName, StatType StatType)[] AssignableStats =
	[
		("AddBiliButton", StatType.Bili),
		("AddShenfaButton", StatType.Shenfa),
		("AddWuxingButton", StatType.Wuxing),
		("AddFuyuanButton", StatType.Fuyuan),
		("AddGenguButton", StatType.Gengu),
		("AddDingliButton", StatType.Dingli),
	];

	private JyButton _addPointButton = null!;
	private Label _pointLabel = null!;
	private Control _assignStatWidget = null!;
	private JyButton _assignStatCloseButton = null!;
	private string _characterId = string.Empty;

	public override void _Ready()
	{
		_addPointButton = GetNode<JyButton>("%AddPointButton");
		_pointLabel = _addPointButton.GetNode<Label>("PointLabel");
		_assignStatWidget = GetNode<Control>("AssignStatWidget");
		_assignStatCloseButton = _assignStatWidget.GetNode<JyButton>("CloseButton");

		_addPointButton.Pressed += OnAddPointButtonPressed;
		_assignStatCloseButton.Pressed += HideAssignStatWidget;

		foreach (var (buttonName, statType) in AssignableStats)
		{
			var button = _assignStatWidget.GetNode<JyButton>($"HBoxContainer/{buttonName}");
			button.Pressed += () => OnAssignStatButtonPressed(statType);
		}

		HideAssignStatWidget();
		_addPointButton.Disabled = true;
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		_characterId = character.Id;
		_pointLabel.Text = character.UnspentStatPoints.ToString();
		_addPointButton.Disabled = character.UnspentStatPoints <= 0;

		if (character.UnspentStatPoints <= 0)
		{
			HideAssignStatWidget();
		}

		foreach (var (statType, nodeName) in DisplayedStats)
		{
			var label = GetNode<Label>($"%{nodeName}");
			var baseValue = character.GetBaseStat(statType);
			var finalValue = Mathf.RoundToInt(character.GetStat(statType));
			label.Text = $"{baseValue}(+{finalValue - baseValue})";
		}
	}

	private void OnAddPointButtonPressed()
	{
		if (_addPointButton.Disabled)
		{
			return;
		}

		_assignStatWidget.Show();
	}

	private void OnAssignStatButtonPressed(StatType statType)
	{
		if (string.IsNullOrWhiteSpace(_characterId))
		{
			throw new InvalidOperationException("CharacterAttributeTab is not initialized with a character.");
		}

		Game.CharacterService.AllocateStat(_characterId, statType);
	}

	private void HideAssignStatWidget()
	{
		_assignStatWidget.Hide();
	}
}
