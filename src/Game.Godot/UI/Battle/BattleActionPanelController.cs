using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed record BattleActionPanelView(
	BattleSelectedSkillBox SelectedSkillBox,
	BaseButton MoveButton,
	BaseButton StatusButton,
	BaseButton ItemButton,
	BaseButton RestButton,
	BaseButton EndButton,
	BaseButton SurrenderButton,
	TextureRect Avatar,
	HBoxContainer ListContainer,
	Control OverlayRoot);

internal sealed record BattleActionPanelScenes(
	PackedScene SkillBox,
	PackedScene ItemPanel,
	PackedScene StatusPanel);

internal sealed class BattleActionPanelController(
	BattlePresenter presenter,
	BattleUiStateMachine uiState,
	int playerTeam,
	Func<BattleState?> getState,
	Func<BattleFlowOrchestrator?> getOrchestrator,
	Func<bool> isResolvingPresentation,
	Func<bool> isAutoBattleEnabled,
	Action<bool> refreshAll,
	BattleActionPanelView view,
	BattleActionPanelScenes scenes)
{
	private ButtonGroup _skillButtonGroup = new();

	public void BindButtons()
	{
		view.MoveButton.Pressed += async () => await SelectMoveAsync();
		view.StatusButton.Pressed += OpenStatusPanel;
		view.ItemButton.Pressed += async () => await OpenItemPanelAsync();
		view.RestButton.Pressed += async () =>
		{
			if (getOrchestrator() is { } orchestrator)
			{
				await orchestrator.TryRestAsync();
			}
		};
		view.EndButton.Pressed += async () =>
		{
			if (getOrchestrator() is { } orchestrator)
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
		var state = getState();
		if (state is null)
		{
			view.SelectedSkillBox.Setup(null);
			return;
		}

		view.SelectedSkillBox.Setup(ResolvePreviewSkill(BattlePresenter.TryGetActingUnit(state)));
	}

	public void RefreshActions()
	{
		var state = getState();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		var isActing = actingUnit is not null &&
			actingUnit.Team == playerTeam &&
			uiState.Mode != BattleUiMode.BattleEnded &&
			!isResolvingPresentation() &&
			!isAutoBattleEnabled();
		view.MoveButton.Disabled = !isActing;
		view.StatusButton.Disabled = state is null || uiState.Mode == BattleUiMode.BattleEnded || isResolvingPresentation();
		view.ItemButton.Disabled = !isActing;
		view.RestButton.Disabled = !isActing;
		view.EndButton.Disabled = !isActing;
		view.SurrenderButton.Disabled = uiState.Mode == BattleUiMode.BattleEnded;
		view.SurrenderButton.Visible = uiState.Mode != BattleUiMode.BattleEnded;
	}

	public void RefreshList()
	{
		ClearChildren(view.ListContainer);
		_skillButtonGroup = new ButtonGroup();
		var state = getState();
		if (state is null)
		{
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(state);
		if (actingUnit is null || actingUnit.Team != playerTeam)
		{
			AddListLabel("无可选项");
			return;
		}

		if (uiState.Mode == BattleUiMode.SelectingItem)
		{
			AddListLabel("从物品面板中选择消耗品。");
			return;
		}

		if (uiState.Mode == BattleUiMode.SelectingItemTarget)
		{
			AddListLabel(uiState.SelectedItem is null ? "点击高亮目标使用。" : $"使用：{uiState.SelectedItem.Definition.Name}");
			return;
		}

		foreach (var skillView in GetSkillOptions(actingUnit))
		{
			AddSkillButton(skillView);
		}
	}

	public void RefreshAvatar()
	{
		var state = getState();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		view.Avatar.Texture = actingUnit is null ? null : AssetResolver.LoadCharacterPortrait(actingUnit.Character);
	}

	public void SelectDefaultPostMoveMode(BattleUnit actingUnit)
	{
		var defaultSkill = ResolveDefaultSkill(actingUnit, GetSkillOptions(actingUnit));
		if (defaultSkill is null)
		{
			uiState.ActUnit();
			return;
		}

		uiState.SelectSkillTarget(defaultSkill);
	}

	private async Task SelectMoveAsync()
	{
		if (getOrchestrator() is not { } orchestrator)
		{
			return;
		}

		var state = getState();
		if (state is not null && BattlePresenter.TryGetActingUnit(state) is not null &&
			state.CurrentAction is { HasMoved: true, HasCommittedMainAction: false })
		{
			await orchestrator.TryRollbackMoveAsync();
		}

		uiState.SelectMove();
		refreshAll(true);
	}

	private async Task OpenItemPanelAsync()
	{
		var state = getState();
		if (state is null || BattlePresenter.TryGetActingUnit(state) is null)
		{
			return;
		}

		var itemEntries = presenter.CreateItemList(GameRoot.State.Inventory).Select(static view => view.Entry).ToArray();
		if (itemEntries.Length == 0)
		{
			UIRoot.Instance.ShowSuggestion("当前没有可用的消耗品。");
			return;
		}

		uiState.SelectItemList();
		refreshAll(true);
		var instance = scenes.ItemPanel.Instantiate();
		if (instance is not BattleItemPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle item panel scene root must be BattleItemPanel.");
		}

		panel.Configure(itemEntries);
		view.OverlayRoot.AddChild(panel);
		var selectedEntry = await panel.AwaitSelectionAsync();
		if (selectedEntry is null)
		{
			if (uiState.Mode == BattleUiMode.SelectingItem)
			{
				uiState.ActUnit();
				refreshAll(true);
			}
			return;
		}

		uiState.SelectItemTarget(selectedEntry);
		refreshAll(true);
	}

	private void OpenStatusPanel()
	{
		var state = getState();
		if (state is null || uiState.Mode == BattleUiMode.BattleEnded)
		{
			return;
		}

		var instance = scenes.StatusPanel.Instantiate();
		if (instance is not BattleStatusPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle status panel scene root must be BattleStatusPanel.");
		}

		panel.Configure(state, playerTeam);
		view.OverlayRoot.AddChild(panel);
	}

	private SkillInstance? ResolvePreviewSkill(BattleUnit? actingUnit)
	{
		var skillOptions = actingUnit is null ? [] : GetSkillOptions(actingUnit);
		if (uiState.SelectedSkill is { } selected && skillOptions.Any(view => view.IsAvailable && ReferenceEquals(view.Skill, selected)))
		{
			return selected;
		}
		return actingUnit is null ? null : ResolveDefaultSkill(actingUnit, skillOptions);
	}

	private IReadOnlyList<BattleSkillOptionView> GetSkillOptions(BattleUnit actingUnit)
	{
		var state = getState();
		var orchestrator = getOrchestrator();
		return state is null || orchestrator is null ? [] : presenter.CreateSkillList(state, orchestrator.Engine, actingUnit);
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
		view.ListContainer.AddChild(label);
	}

	private void AddSkillButton(BattleSkillOptionView skillView)
	{
		if (scenes.SkillBox.Instantiate() is not BattleSkillBox button)
		{
			throw new InvalidOperationException("BattleSkillBox scene root must be BattleSkillBox.");
		}
		button.Setup(skillView.Skill, ReferenceEquals(uiState.SelectedSkill, skillView.Skill), skillView.Availability);
		button.ButtonGroup = _skillButtonGroup;
		button.TooltipText = skillView.IsAvailable ? skillView.Label : $"{skillView.Label}\n{ResolveUnavailableSkillText(skillView.Availability)}";
		if (skillView.IsAvailable)
		{
			button.Pressed += () =>
			{
				uiState.SelectSkillTarget(skillView.Skill);
				refreshAll(false);
			};
		}
		view.ListContainer.AddChild(button);
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
