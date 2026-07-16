using Game.Core.Battle;
using Game.Presentation.Battle;
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
	int playerTeam,
	Func<BattleState?> getState,
	Func<BattleEngine?> getEngine,
	Action<BattleUiIntent> dispatch,
	BattleActionPanelView view,
	BattleActionPanelScenes scenes)
{
	private ButtonGroup _skillButtonGroup = new();

	public void BindButtons()
	{
		view.MoveButton.Pressed += () => dispatch(new BattleUiIntent.SelectMove());
		view.StatusButton.Pressed += () => dispatch(new BattleUiIntent.OpenStatus());
		view.ItemButton.Pressed += () => dispatch(new BattleUiIntent.OpenItems());
		view.RestButton.Pressed += () => dispatch(new BattleUiIntent.Rest());
		view.EndButton.Pressed += () => dispatch(new BattleUiIntent.EndAction());
		view.SurrenderButton.Pressed += () => dispatch(new BattleUiIntent.Surrender());
	}

	public void Render(BattleInteractionState interaction, bool refreshList = true)
	{
		RefreshSelectedSkill(interaction);
		RefreshActions(interaction.Capabilities);
		if (refreshList)
		{
			RefreshList(interaction);
		}
		RefreshAvatar();
	}

	public void RefreshActions(BattleUiCapabilities capabilities)
	{
		view.MoveButton.Disabled = !capabilities.CanSelectMove;
		view.StatusButton.Disabled = !capabilities.CanOpenStatus;
		view.ItemButton.Disabled = !capabilities.CanOpenItem;
		view.RestButton.Disabled = !capabilities.CanRest;
		view.EndButton.Disabled = !capabilities.CanEndAction;
		view.SurrenderButton.Disabled = !capabilities.CanSurrender;
		view.SurrenderButton.Visible = capabilities.CanSurrender;
		if (!capabilities.CanSelectSkill)
		{
			foreach (var child in view.ListContainer.GetChildren().OfType<BaseButton>())
			{
				child.Disabled = true;
			}
		}
	}

	public async Task<InventoryEntry?> ShowItemPanelAsync()
	{
		var itemEntries = presenter.CreateItemList(GameRoot.State.Inventory)
			.Select(static item => item.Entry)
			.ToArray();
		if (itemEntries.Length == 0)
		{
			UIRoot.Instance.ShowSuggestion("当前没有可用的消耗品。");
			return null;
		}

		var instance = scenes.ItemPanel.Instantiate();
		if (instance is not BattleItemPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle item panel scene root must be BattleItemPanel.");
		}

		panel.Configure(itemEntries);
		view.OverlayRoot.AddChild(panel);
		return await panel.AwaitSelectionAsync();
	}

	public void ShowStatusPanel()
	{
		var state = getState();
		if (state is null)
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

	private void RefreshSelectedSkill(BattleInteractionState interaction)
	{
		var state = getState();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		view.SelectedSkillBox.Setup(ResolvePreviewSkill(actingUnit, interaction.SelectedSkill));
	}

	private void RefreshList(BattleInteractionState interaction)
	{
		ClearChildren(view.ListContainer);
		_skillButtonGroup = new ButtonGroup();
		var state = getState();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		if (actingUnit is null || actingUnit.Team != playerTeam)
		{
			AddListLabel("无可选项");
			return;
		}

		if (interaction.Kind == BattleFlowStateKind.SelectingItem)
		{
			AddListLabel("从物品面板中选择消耗品。");
			return;
		}

		if (interaction.Kind == BattleFlowStateKind.SelectingItemTarget)
		{
			AddListLabel(interaction.SelectedItem is null
				? "点击高亮目标使用。"
				: $"使用：{interaction.SelectedItem.Definition.Name}");
			return;
		}

		foreach (var skillView in GetSkillOptions(actingUnit))
		{
			AddSkillButton(skillView, interaction);
		}
	}

	private void RefreshAvatar()
	{
		var state = getState();
		var actingUnit = state is null ? null : BattlePresenter.TryGetActingUnit(state);
		view.Avatar.Texture = actingUnit is null ? null : AssetResolver.LoadCharacterPortrait(actingUnit.Character);
	}

	private SkillInstance? ResolvePreviewSkill(BattleUnit? actingUnit, SkillInstance? selectedSkill)
	{
		if (actingUnit is null)
		{
			return null;
		}

		var skillOptions = GetSkillOptions(actingUnit);
		if (selectedSkill is { } selected && skillOptions.Any(view =>
				view.IsAvailable && ReferenceEquals(view.Skill, selected)))
		{
			return selected;
		}

		if (!string.IsNullOrWhiteSpace(actingUnit.LastUsedSkillId))
		{
			var lastUsed = skillOptions.FirstOrDefault(view =>
				view.IsAvailable && view.Skill.Id == actingUnit.LastUsedSkillId);
			if (lastUsed is not null)
			{
				return lastUsed.Skill;
			}
		}

		return skillOptions.FirstOrDefault(static view => view.IsAvailable)?.Skill;
	}

	private IReadOnlyList<BattleSkillOptionView> GetSkillOptions(BattleUnit actingUnit)
	{
		var state = getState();
		var engine = getEngine();
		return state is null || engine is null
			? []
			: presenter.CreateSkillList(state, engine, actingUnit);
	}

	private void AddListLabel(string text)
	{
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", Colors.White);
		view.ListContainer.AddChild(label);
	}

	private void AddSkillButton(BattleSkillOptionView skillView, BattleInteractionState interaction)
	{
		if (scenes.SkillBox.Instantiate() is not BattleSkillBox button)
		{
			throw new InvalidOperationException("BattleSkillBox scene root must be BattleSkillBox.");
		}

		button.Setup(
			skillView.Skill,
			ReferenceEquals(interaction.SelectedSkill, skillView.Skill),
			skillView.Availability);
		button.ButtonGroup = _skillButtonGroup;
		button.TooltipText = skillView.IsAvailable
			? skillView.Label
			: $"{skillView.Label}\n{ResolveUnavailableSkillText(skillView.Availability)}";
		if (skillView.IsAvailable && interaction.Capabilities.CanSelectSkill)
		{
			button.Pressed += () => dispatch(new BattleUiIntent.SelectSkill(skillView.Skill));
		}
		else
		{
			button.Disabled = true;
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
