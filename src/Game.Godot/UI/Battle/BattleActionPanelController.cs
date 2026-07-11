using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleActionPanelController
{
	private readonly BattlePresenter _presenter;
	private readonly BattleUiStateMachine _uiState;
	private readonly int _playerTeam;
	private readonly Func<BattleState?> _state;
	private readonly Func<BattleFlowOrchestrator?> _orchestrator;
	private readonly Func<bool> _isResolvingPresentation;
	private readonly Func<bool> _isAutoBattleEnabled;
	private readonly Action<bool> _refreshAll;
	private readonly BattleSelectedSkillBox _selectedSkillBox;
	private readonly BaseButton _moveButton;
	private readonly BaseButton _statusButton;
	private readonly BaseButton _itemButton;
	private readonly BaseButton _restButton;
	private readonly BaseButton _endButton;
	private readonly BaseButton _surrenderButton;
	private readonly TextureRect _avatar;
	private readonly HBoxContainer _listContainer;
	private readonly Control _overlayRoot;
	private readonly PackedScene _skillBoxScene;
	private readonly PackedScene _itemPanelScene;
	private readonly PackedScene _statusPanelScene;
	private ButtonGroup _skillButtonGroup = new();

	public BattleActionPanelController(
		BattlePresenter presenter,
		BattleUiStateMachine uiState,
		int playerTeam,
		Func<BattleState?> state,
		Func<BattleFlowOrchestrator?> orchestrator,
		Func<bool> isResolvingPresentation,
		Func<bool> isAutoBattleEnabled,
		Action<bool> refreshAll,
		BattleSelectedSkillBox selectedSkillBox,
		BaseButton moveButton,
		BaseButton statusButton,
		BaseButton itemButton,
		BaseButton restButton,
		BaseButton endButton,
		BaseButton surrenderButton,
		TextureRect avatar,
		HBoxContainer listContainer,
		Control overlayRoot,
		PackedScene skillBoxScene,
		PackedScene itemPanelScene,
		PackedScene statusPanelScene)
	{
		_presenter = presenter;
		_uiState = uiState;
		_playerTeam = playerTeam;
		_state = state;
		_orchestrator = orchestrator;
		_isResolvingPresentation = isResolvingPresentation;
		_isAutoBattleEnabled = isAutoBattleEnabled;
		_refreshAll = refreshAll;
		_selectedSkillBox = selectedSkillBox;
		_moveButton = moveButton;
		_statusButton = statusButton;
		_itemButton = itemButton;
		_restButton = restButton;
		_endButton = endButton;
		_surrenderButton = surrenderButton;
		_avatar = avatar;
		_listContainer = listContainer;
		_overlayRoot = overlayRoot;
		_skillBoxScene = skillBoxScene;
		_itemPanelScene = itemPanelScene;
		_statusPanelScene = statusPanelScene;
	}

	public void BindButtons()
	{
		_moveButton.Pressed += async () => await SelectMoveAsync();
		_statusButton.Pressed += OpenStatusPanel;
		_itemButton.Pressed += async () => await OpenItemPanelAsync();
		_restButton.Pressed += async () =>
		{
			if (_orchestrator() is { } orchestrator)
			{
				await orchestrator.TryRestAsync();
			}
		};
		_endButton.Pressed += async () =>
		{
			if (_orchestrator() is { } orchestrator)
			{
				await orchestrator.TryEndActionAsync();
			}
		};
	}

	public void Refresh()
	{
		RefreshSelectedSkill();
		RefreshActions();
		RefreshList();
		RefreshAvatar();
	}

	public void RefreshSelectedSkill()
	{
		var state = _state();
		if (state is null)
		{
			_selectedSkillBox.Setup(null);
			return;
		}

		_selectedSkillBox.Setup(ResolvePreviewSkill(BattlePresenter.TryGetActingUnit(state)));
	}

	public void RefreshActions()
	{
		var state = _state();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		var isActing = actingUnit is not null &&
			actingUnit.Team == _playerTeam &&
			_uiState.Mode != BattleUiMode.BattleEnded &&
			!_isResolvingPresentation() &&
			!_isAutoBattleEnabled();
		_moveButton.Disabled = !isActing;
		_statusButton.Disabled = state is null || _uiState.Mode == BattleUiMode.BattleEnded || _isResolvingPresentation();
		_itemButton.Disabled = !isActing;
		_restButton.Disabled = !isActing;
		_endButton.Disabled = !isActing;
		_surrenderButton.Disabled = _uiState.Mode == BattleUiMode.BattleEnded;
		_surrenderButton.Visible = _uiState.Mode != BattleUiMode.BattleEnded;
	}

	public void RefreshList()
	{
		ClearChildren(_listContainer);
		_skillButtonGroup = new ButtonGroup();
		var state = _state();
		if (state is null)
		{
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(state);
		if (actingUnit is null || actingUnit.Team != _playerTeam)
		{
			AddListLabel("无可选项");
			return;
		}

		if (_uiState.Mode == BattleUiMode.SelectingItem)
		{
			AddListLabel("从物品面板中选择消耗品。");
			return;
		}

		if (_uiState.Mode == BattleUiMode.SelectingItemTarget)
		{
			AddListLabel(_uiState.SelectedItem is null ? "点击高亮目标使用。" : $"使用：{_uiState.SelectedItem.Definition.Name}");
			return;
		}

		foreach (var skillView in GetSkillOptions(actingUnit))
		{
			AddSkillButton(skillView);
		}
	}

	public void RefreshAvatar()
	{
		var state = _state();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		_avatar.Texture = actingUnit is null ? null : AssetResolver.LoadCharacterPortrait(actingUnit.Character);
	}

	public void SelectDefaultPostMoveMode(BattleUnit actingUnit)
	{
		var defaultSkill = ResolveDefaultSkill(actingUnit, GetSkillOptions(actingUnit));
		if (defaultSkill is null)
		{
			_uiState.ActUnit();
			return;
		}

		_uiState.SelectSkillTarget(defaultSkill);
	}

	private async Task SelectMoveAsync()
	{
		if (_orchestrator() is not { } orchestrator)
		{
			return;
		}

		var state = _state();
		if (state is not null && BattlePresenter.TryGetActingUnit(state) is not null &&
			state.CurrentAction is { HasMoved: true, HasCommittedMainAction: false })
		{
			await orchestrator.TryRollbackMoveAsync();
		}

		_uiState.SelectMove();
		_refreshAll(true);
	}

	private async Task OpenItemPanelAsync()
	{
		var state = _state();
		if (state is null || BattlePresenter.TryGetActingUnit(state) is null)
		{
			return;
		}

		var itemEntries = _presenter.CreateItemList(GameRoot.State.Inventory).Select(static view => view.Entry).ToArray();
		if (itemEntries.Length == 0)
		{
			UIRoot.Instance.ShowSuggestion("当前没有可用的消耗品。");
			return;
		}

		_uiState.SelectItemList();
		_refreshAll(true);
		var instance = _itemPanelScene.Instantiate();
		if (instance is not BattleItemPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle item panel scene root must be BattleItemPanel.");
		}

		panel.Configure(itemEntries);
		_overlayRoot.AddChild(panel);
		var selectedEntry = await panel.AwaitSelectionAsync();
		if (selectedEntry is null)
		{
			if (_uiState.Mode == BattleUiMode.SelectingItem)
			{
				_uiState.ActUnit();
				_refreshAll(true);
			}
			return;
		}

		_uiState.SelectItemTarget(selectedEntry);
		_refreshAll(true);
	}

	private void OpenStatusPanel()
	{
		var state = _state();
		if (state is null || _uiState.Mode == BattleUiMode.BattleEnded)
		{
			return;
		}

		var instance = _statusPanelScene.Instantiate();
		if (instance is not BattleStatusPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle status panel scene root must be BattleStatusPanel.");
		}

		panel.Configure(state, _playerTeam);
		_overlayRoot.AddChild(panel);
	}

	private SkillInstance? ResolvePreviewSkill(BattleUnit? actingUnit)
	{
		var skillOptions = actingUnit is null ? [] : GetSkillOptions(actingUnit);
		if (_uiState.SelectedSkill is { } selected && skillOptions.Any(view => view.IsAvailable && ReferenceEquals(view.Skill, selected)))
		{
			return selected;
		}
		return actingUnit is null ? null : ResolveDefaultSkill(actingUnit, skillOptions);
	}

	private IReadOnlyList<BattleSkillOptionView> GetSkillOptions(BattleUnit actingUnit)
	{
		var state = _state();
		var orchestrator = _orchestrator();
		return state is null || orchestrator is null ? [] : _presenter.CreateSkillList(state, orchestrator.Engine, actingUnit);
	}

	private static SkillInstance? ResolveDefaultSkill(BattleUnit actingUnit, IReadOnlyList<BattleSkillOptionView> skillOptions)
	{
		if (!string.IsNullOrWhiteSpace(actingUnit.LastUsedSkillId))
		{
			var lastUsed = skillOptions.FirstOrDefault(view => view.IsAvailable && view.Skill.Id == actingUnit.LastUsedSkillId);
			if (lastUsed is not null)
			{
				return lastUsed.Skill;
			}
		}
		return skillOptions.FirstOrDefault(static view => view.IsAvailable)?.Skill;
	}

	private void AddListLabel(string text)
	{
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", Colors.White);
		_listContainer.AddChild(label);
	}

	private void AddSkillButton(BattleSkillOptionView skillView)
	{
		if (_skillBoxScene.Instantiate() is not BattleSkillBox button)
		{
			throw new InvalidOperationException("BattleSkillBox scene root must be BattleSkillBox.");
		}
		button.Setup(skillView.Skill, ReferenceEquals(_uiState.SelectedSkill, skillView.Skill), skillView.Availability);
		button.ButtonGroup = _skillButtonGroup;
		button.TooltipText = skillView.IsAvailable ? skillView.Label : $"{skillView.Label}\n{ResolveUnavailableSkillText(skillView.Availability)}";
		if (skillView.IsAvailable)
		{
			button.Pressed += () =>
			{
				_uiState.SelectSkillTarget(skillView.Skill);
				_refreshAll(false);
			};
		}
		_listContainer.AddChild(button);
	}

	private static string ResolveUnavailableSkillText(BattleSkillAvailability availability) => availability.Status switch
	{
		BattleSkillAvailabilityStatus.Cooldown => $"不可用：冷却中（剩余 {availability.RemainingCooldown} 回合）",
		BattleSkillAvailabilityStatus.Disabled => "不可用：当前被封招",
		BattleSkillAvailabilityStatus.NotEnoughMp => "不可用：MP 不足",
		BattleSkillAvailabilityStatus.NotEnoughRage => "不可用：怒气不足",
		_ => "可用",
	};

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			child.QueueFree();
		}
	}
}
