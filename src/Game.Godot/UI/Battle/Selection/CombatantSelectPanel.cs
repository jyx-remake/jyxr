using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CombatantSelectPanel : JyPanel
{
	private const int PlayerTeam = 1;

	[Export]
	public PackedScene CharacterCardScene { get; set; } = null!;

	[Export]
	public string PreviewBattleId { get; set; } = string.Empty;

	private GridContainer _gridContainer = null!;
	private Label _titleLabel = null!;
	private Label _capacityLabel = null!;
	private Label _selectedCountLabel = null!;
	private Label _warningLabel = null!;
	private TextureButton _confirmButton = null!;
	private Label _confirmButtonLabel = null!;

	private readonly HashSet<string> _requiredIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> _selectedIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, CombatantSelectCard> _cards = new(StringComparer.Ordinal);
	private string _battleId = string.Empty;
	private int _deploySlotCount;
	private bool _isConfigured;
	private readonly TaskCompletionSource<IReadOnlyList<string>> _deploymentCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	public IReadOnlyList<string> SelectedCharacterIds => GetSelectedCharacterIds();

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_capacityLabel = GetNode<Label>("%CapacityLabel");
		_selectedCountLabel = GetNode<Label>("%SelectedCountLabel");
		_warningLabel = GetNode<Label>("%WarningLabel");
		_confirmButton = GetNode<TextureButton>("%ConfirmButton");
		_confirmButtonLabel = GetNode<Label>("%ConfirmButtonLabel");
		_confirmButton.Pressed += ConfirmSelection;

		ClearGrid();
		if (!string.IsNullOrWhiteSpace(PreviewBattleId))
		{
			Configure(PreviewBattleId);
			return;
		}

		Refresh();
	}

	public void Configure(string battleId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
		Configure(Game.ContentRepository.GetBattle(battleId));
	}

	public void Configure(BattleDefinition battle)
	{
		ArgumentNullException.ThrowIfNull(battle);

		_battleId = battle.Id;
		_deploySlotCount = CountPlayerDeploySlots(battle);
		_requiredIds.Clear();
		_selectedIds.Clear();
		foreach (var characterId in battle.RequiredCharacterIds)
		{
			if (string.IsNullOrWhiteSpace(characterId))
			{
				continue;
			}

			_requiredIds.Add(characterId);
			_selectedIds.Add(characterId);
		}

		_isConfigured = true;
		Refresh();
	}

	public async Task<IReadOnlyList<string>> AwaitDeploymentAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_deploymentCompletion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _deploymentCompletion.Task;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!_deploymentCompletion.Task.IsCompleted)
		{
			_deploymentCompletion.TrySetCanceled();
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree() || !_isConfigured)
		{
			return;
		}

		ClearGrid();
		_cards.Clear();

		_titleLabel.Text = string.IsNullOrWhiteSpace(_battleId)
			? "出战人物选择"
			: $"出战人物选择 - {_battleId}";

		var members = Game.State.Party.Members;
		foreach (var member in members)
		{
			var isRequired = _requiredIds.Contains(member.Id);
			var isSelected = _selectedIds.Contains(member.Id);
			var card = CreateCard(member, isSelected, isRequired);
			_gridContainer.AddChild(card);
			_cards[member.Id] = card;
		}

		RefreshFooter();
	}

	private CombatantSelectCard CreateCard(
		CharacterInstance character,
		bool isSelected,
		bool isRequired)
	{
		if (CharacterCardScene is null)
		{
			throw new InvalidOperationException("Combatant select card scene is not assigned.");
		}

		var instance = CharacterCardScene.Instantiate();
		if (instance is not CombatantSelectCard card)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Combatant select card scene root must be CombatantSelectCard.");
		}

		card.Setup(character, isSelected, isRequired);
		card.SelectionRequested += ToggleSelection;
		return card;
	}

	private void ToggleSelection(string characterId)
	{
		if (_requiredIds.Contains(characterId))
		{
			return;
		}

		if (_selectedIds.Contains(characterId))
		{
			_selectedIds.Remove(characterId);
			SetCardSelected(characterId, false);
			RefreshFooter();
			return;
		}

		if (_selectedIds.Count >= _deploySlotCount)
		{
			RefreshFooter("出战人数已满。");
			return;
		}

		_selectedIds.Add(characterId);
		SetCardSelected(characterId, true);
		RefreshFooter();
	}

	private void ConfirmSelection()
	{
		var validationMessage = GetValidationMessage();
		if (!string.IsNullOrEmpty(validationMessage))
		{
			RefreshFooter(validationMessage);
			return;
		}

		var selected = GetSelectedCharacterIds();
		Game.Logger.Info($"Battle deployment selected for '{_battleId}': {string.Join(", ", selected)}");
		if (_deploymentCompletion.TrySetResult(selected))
		{
			QueueFree();
		}
	}

	private void RefreshFooter(string? transientWarning = null)
	{
		_capacityLabel.Text = $"可选择{_deploySlotCount}人";
		_selectedCountLabel.Text = $"已选择{_selectedIds.Count}人";

		var warning = transientWarning ?? GetValidationMessage();
		_warningLabel.Text = warning;
		_warningLabel.Visible = !string.IsNullOrWhiteSpace(warning);

		var canConfirm = string.IsNullOrEmpty(GetValidationMessage());
		_confirmButton.Disabled = !canConfirm;
		_confirmButton.Modulate = canConfirm ? Colors.White : new Color(0.55f, 0.55f, 0.55f, 0.8f);
		_confirmButtonLabel.Modulate = canConfirm ? Colors.White : new Color(0.75f, 0.75f, 0.75f, 1f);
	}

	private string GetValidationMessage()
	{
		if (_deploySlotCount <= 0)
		{
			return "当前战斗没有可部署的玩家位置。";
		}

		if (_requiredIds.Count > _deploySlotCount)
		{
			return "必选角色数量超过可部署人数。";
		}

		var missingRequired = _requiredIds
			.Where(requiredId => !Game.State.Party.ContainsMember(requiredId))
			.ToArray();
		if (missingRequired.Length > 0)
		{
			return $"必选角色不在当前队伍：{string.Join("、", missingRequired)}";
		}

		if (_selectedIds.Count == 0)
		{
			return "至少选择1名角色。";
		}

		if (_selectedIds.Count > _deploySlotCount)
		{
			return "已选择人数超过可部署人数。";
		}

		return string.Empty;
	}

	private void SetCardSelected(string characterId, bool selected)
	{
		if (_cards.TryGetValue(characterId, out var card))
		{
			card.SetSelected(selected);
		}
	}

	private IReadOnlyList<string> GetSelectedCharacterIds() =>
		Game.State.Party.Members
			.Where(member => _selectedIds.Contains(member.Id))
			.Select(member => member.Id)
			.ToArray();

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static int CountPlayerDeploySlots(BattleDefinition battle) =>
		battle.Participants.Count(static participant =>
			participant.Team == PlayerTeam &&
			participant.PartyIndex is not null &&
			participant.CharacterId is null);
}
