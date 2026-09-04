using Game.Application;
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
	private JyButton _leaveButton = null!;
	private string _characterId = string.Empty;
	private bool _isReadOnly;
	private bool _isLeaveInProgress;

	/// <summary>
	/// Raised after the displayed character was kicked from the party, so the
	/// hosting panel can switch to another character.
	/// </summary>
	public event Action<string>? CharacterKicked;

	public bool IsReadOnly
	{
		get => _isReadOnly;
		set
		{
			_isReadOnly = value;
			if (_isReadOnly && IsInsideTree())
			{
				HideAssignStatWidget();
			}
		}
	}

	public override void _Ready()
	{
		_addPointButton = GetNode<JyButton>("%AddPointButton");
		_pointLabel = GetNode<Label>("%PointLabel");
		_assignStatWidget = GetNode<Control>("%AssignStatWidget");
		_assignStatCloseButton = GetNode<JyButton>("%CloseButton");
		_leaveButton = GetNode<JyButton>("%LeaveButton");

		_addPointButton.Pressed += OnAddPointButtonPressed;
		_assignStatCloseButton.Pressed += HideAssignStatWidget;
		_leaveButton.Pressed += OnLeaveButtonPressed;

		foreach (var (buttonName, statType) in AssignableStats)
		{
			var button = GetNode<JyButton>($"%{buttonName}");
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
		_addPointButton.Disabled = IsReadOnly || character.UnspentStatPoints <= 0;

		if (IsReadOnly || character.UnspentStatPoints <= 0)
		{
			HideAssignStatWidget();
		}

		// The hero can never be kicked (legacy ButtonLidui guards 主角 too).
		_leaveButton.Visible = !IsReadOnly &&
			!string.Equals(character.Id, Party.HeroCharacterId, StringComparison.Ordinal);

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
		if (IsReadOnly || _addPointButton.Disabled)
		{
			return;
		}

		_assignStatWidget.Show();
	}

	private void OnAssignStatButtonPressed(StatType statType)
	{
		if (IsReadOnly)
		{
			return;
		}

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

	private async void OnLeaveButtonPressed()
	{
		if (_isLeaveInProgress || IsReadOnly || string.IsNullOrWhiteSpace(_characterId))
		{
			return;
		}

		var character = Game.State.Party.TryGetMember(_characterId, out var member) ? member : null;
		if (character is null)
		{
			return;
		}

		_isLeaveInProgress = true;
		try
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync(
				$"确认让【{character.Name}】离队吗？离队后可随时将其召回队伍。",
				ConfirmDialogTone.Warning);
			if (!confirmed)
			{
				return;
			}

			Game.PartyService.Kick(_characterId);
			CharacterKicked?.Invoke(_characterId);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Kicking party member failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
		finally
		{
			_isLeaveInProgress = false;
		}
	}
}
