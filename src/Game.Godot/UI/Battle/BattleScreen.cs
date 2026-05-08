using Game.Core.Battle;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Godot.Audio;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

public partial class BattleScreen : Control
{
	private const int PlayerTeam = 1;
	private const int GridCellWidth = 144;
	private const int GridCellHeight = 144;
	private static readonly Color DefaultCellColor = new(0.2f, 0.2f, 0.2f, 0.2f);
	private static readonly Color MoveHighlightColor = new(0.2f, 0.6f, 1f, 0.35f);
	private static readonly Color ActingUnitColor = new(1f, 1f, 0.2f, 0.5f);
	private static readonly Color PlayerUnitColor = new(0.2f, 1f, 0.2f, 0.25f);
	private static readonly Color EnemyUnitColor = new(1f, 0.2f, 0.2f, 0.25f);
	private static readonly Color SkillTargetColor = new(1f, 0.95f, 0.55f, 0.8f);
	private static readonly Color SkillPossibleImpactColor = new(0.2f, 0.2f, 0.2f, 0.5f);
	private static readonly Color SkillActualImpactColor = new(1f, 0.3f, 0.2f, 1f);
	private static readonly Color FloatDamageColor = Colors.White;
	private static readonly Color FloatCriticalColor = Colors.Yellow;
	private static readonly Color FloatHealColor = Colors.Green;
	private static readonly Color FloatManaColor = Colors.Blue;
	private static readonly Color FloatStateColor = Colors.Red;
	private static readonly Color FloatSpecialColor = Colors.Magenta;
	private static readonly Color FloatInfoColor = Colors.Yellow;

	[Export]
	public PackedScene BattleSkillBoxScene { get; set; } = null!;

	[Export]
	public PackedScene BattleItemPanelScene { get; set; } = null!;

	private readonly BattleEngine _engine = new(buffResolver: buffId => GameRoot.ContentRepository.GetBuff(buffId));
	private readonly BattlePresenter _presenter = new();
	private readonly BattleUiStateMachine _uiState = new();
	private readonly TaskCompletionSource<bool> _battleCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly List<string> _logLines = [];

	private BattleDefinition? _battleDefinition;
	private IReadOnlyList<string> _selectedCharacterIds = [];
	private BattleState? _state;
	private bool _isConfigured;
	private GridPosition? _hoveredCellPosition;

	private TextureRect _background = null!;
	private Label _titleLabel = null!;
	private Label _subtitleLabel = null!;
	private BattleBoardView _boardGrid = null!;
	private TextureRect _selectedSkillIcon = null!;
	private Label _selectedSkillNameLabel = null!;
	private Label _selectedSkillFormNameLabel = null!;
	private BaseButton _moveButton = null!;
	private BaseButton _skillButton = null!;
	private BaseButton _itemButton = null!;
	private BaseButton _restButton = null!;
	private BaseButton _endButton = null!;
	private TextureRect _avatar = null!;
	private HBoxContainer _listContainer = null!;
	private RichTextLabel _logLabel = null!;
	private Button _finishButton = null!;
	private Control _overlayRoot = null!;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("%Background");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_boardGrid = GetNode<BattleBoardView>("%BoardGrid");
		_selectedSkillIcon = GetNode<TextureRect>("%SelectedSkillIcon");
		_selectedSkillNameLabel = GetNode<Label>("%SelectedSkillNameLabel");
		_selectedSkillFormNameLabel = GetNode<Label>("%SelectedSkillFormNameLabel");
		_moveButton = GetNode<BaseButton>("%MoveButton");
		_skillButton = GetNode<BaseButton>("%SkillButton");
		_itemButton = GetNode<BaseButton>("%ItemButton");
		_restButton = GetNode<BaseButton>("%RestButton");
		_endButton = GetNode<BaseButton>("%EndButton");
		_avatar = GetNode<TextureRect>("%Avatar");
		_listContainer = GetNode<HBoxContainer>("%ListContainer");
		_logLabel = GetNode<RichTextLabel>("%LogLabel");
		_finishButton = GetNode<Button>("%FinishButton");
		_overlayRoot = GetNode<Control>("%OverlayRoot");
		_boardGrid.CellPressed += OnCellPressed;
		_boardGrid.HoveredCellChanged += OnBoardHoveredCellChanged;

		_moveButton.Pressed += () =>
		{
			if (_state is not null &&
				BattlePresenter.TryGetActingUnit(_state) is { } unit &&
				_state.CurrentAction is { HasMoved: true, HasCommittedMainAction: false })
			{
				var eventStart = _state.Events.Count;
				var rollbackResult = _engine.RollbackMove(_state, unit.Id);
				AppendResult(rollbackResult, eventStart);
				if (!rollbackResult.Success)
				{
					RefreshAll();
					return;
				}
			}

			_uiState.SelectMove();
			RefreshAll();
		};
		_skillButton.Pressed += () =>
		{
			_uiState.SelectSkillList();
			RefreshAll();
		};
		_itemButton.Pressed += async () => await OpenItemPanelAsync();
		_restButton.Pressed += () =>
		{
			if (_state is null || BattlePresenter.TryGetActingUnit(_state) is not { } unit)
			{
				return;
			}

			var eventStart = _state.Events.Count;
			AppendResult(_engine.Rest(_state, unit.Id), eventStart);
			AdvanceToNextPlayerAction();
		};
		_endButton.Pressed += () =>
		{
			if (_state is null || BattlePresenter.TryGetActingUnit(_state) is not { } unit)
			{
				return;
			}

			var eventStart = _state.Events.Count;
			AppendResult(_engine.EndAction(_state, unit.Id), eventStart);
			AdvanceToNextPlayerAction();
		};
		_finishButton.Pressed += FinishBattle;

		if (_isConfigured)
		{
			StartBattle();
		}
	}

	public void Configure(string battleId, IReadOnlyList<string> selectedCharacterIds)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
		ArgumentNullException.ThrowIfNull(selectedCharacterIds);

		_battleDefinition = GameRoot.ContentRepository.GetBattle(battleId);
		_selectedCharacterIds = selectedCharacterIds.ToArray();
		_isConfigured = true;

		if (IsInsideTree())
		{
			StartBattle();
		}
	}

	public async Task<bool> AwaitBattleAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_battleCompletion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _battleCompletion.Task;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!_battleCompletion.Task.IsCompleted)
		{
			_battleCompletion.TrySetCanceled();
		}
	}

	private void StartBattle()
	{
		if (_battleDefinition is null)
		{
			throw new InvalidOperationException("Battle screen has not been configured.");
		}

		ApplyBattlePresentation(_battleDefinition);
		_state = GameRoot.BattleService.BuildBattleState(_battleDefinition, _selectedCharacterIds);
		_logLines.Clear();
		AppendLog($"战斗开始：{_battleDefinition.Name}");
		_uiState.WaitTimeline();
		AdvanceToNextPlayerAction();
	}

	private void ApplyBattlePresentation(BattleDefinition battle)
	{
		_background.Texture = AssetResolver.LoadBattleBackgroundResource(battle.MapId);
		if (!string.IsNullOrWhiteSpace(battle.Music))
		{
			GameRoot.Audio.PlayBgm(battle.Music);
			return;
		}

		GameRoot.Audio.PlayBgm(GameRoot.Config.RandomBattleMusics);
	}

	private void AdvanceToNextPlayerAction()
	{
		if (_state is null || CompleteIfBattleEnded())
		{
			return;
		}

		_uiState.WaitTimeline();
		var actingUnit = _engine.AdvanceUntilNextAction(_state);
		while (actingUnit.Team != PlayerTeam)
		{
			AppendLog($"{actingUnit.Character.Name} 观望。");
			_engine.EndAction(_state, actingUnit.Id);
			if (CompleteIfBattleEnded())
			{
				return;
			}

			actingUnit = _engine.AdvanceUntilNextAction(_state);
		}

		_uiState.SelectMove();
		AppendLog($"轮到 {actingUnit.Character.Name} 行动。");
		RefreshAll();
	}

	private bool CompleteIfBattleEnded()
	{
		if (_state is null)
		{
			return false;
		}

		var playerAlive = _state.Units.Any(static unit => unit.Team == PlayerTeam && unit.IsAlive);
		var enemyAlive = _state.Units.Any(static unit => unit.Team != PlayerTeam && unit.IsAlive);
		if (playerAlive && enemyAlive)
		{
			return false;
		}

		var isWin = playerAlive;
		_uiState.EndBattle();
		AppendLog(isWin ? "战斗胜利。" : "战斗失败。");
		RefreshAll();
		_finishButton.Text = isWin ? "胜利返回" : "失败返回";
		_finishButton.Disabled = false;
		return true;
	}

	private void RefreshAll()
	{
		if (_state is null || !IsInsideTree())
		{
			return;
		}

		if (_uiState.Mode != BattleUiMode.SelectingSkillTarget)
		{
			_hoveredCellPosition = null;
		}

		var header = _presenter.CreateHeader(_state, _uiState.Mode);
		_titleLabel.Text = _battleDefinition?.Name ?? header.Title;
		_subtitleLabel.Text = header.Subtitle;

		RefreshBoard();
		RefreshSelectedSkill();
		RefreshActions();
		RefreshList();
		RefreshAvatar();
		RefreshLog();
	}

	private void RefreshSelectedSkill()
	{
		if (_state is null)
		{
			_selectedSkillIcon.Texture = null;
			_selectedSkillNameLabel.Text = "未选中技能";
			_selectedSkillFormNameLabel.Text = string.Empty;
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		var previewSkill = ResolvePreviewSkill(actingUnit);
		if (previewSkill is null)
		{
			_selectedSkillIcon.Texture = null;
			_selectedSkillNameLabel.Text = "无可用技能";
			_selectedSkillFormNameLabel.Text = string.Empty;
			return;
		}

		_selectedSkillIcon.Texture = AssetResolver.LoadSkillIconResource(previewSkill.Icon);
		ApplyLegacySkillName(_selectedSkillNameLabel, _selectedSkillFormNameLabel, previewSkill.Name);
	}

	private void RefreshBoard()
	{
		if (_state is null)
		{
			return;
		}

		var highlights = ResolveBoardHighlights();
		var cells = _presenter.CreateCells(_state)
			.Select(cell => new BattleBoardCellVisual(
				cell.Position,
				cell.Label,
				ResolveCellColor(cell, highlights),
				CanClickCell(cell.Position, highlights)))
			.ToArray();
		_boardGrid.RenderGrid(_state.Grid.Width, _state.Grid.Height, GridCellWidth, GridCellHeight, 6, cells);
		var actingUnitId = _state.CurrentAction?.ActingUnitId;
		_boardGrid.RenderUnits(_state.Units
			.Select(unit => new BattleBoardUnitVisual(
				unit.Id,
				unit.Character.Name,
				unit.Position,
				unit.Facing,
				AssetResolver.LoadCombatantAnimation(unit.Character),
				string.Equals(unit.Id, actingUnitId, StringComparison.Ordinal),
				unit.IsAlive,
				unit.Team == PlayerTeam,
				unit.Hp,
				unit.MaxHp,
				unit.Mp,
				unit.MaxMp,
				unit.Rage,
				(int)Math.Round(unit.ActionGauge, MidpointRounding.AwayFromZero),
				AssetResolver.LoadCharacterPortrait(unit.Character),
				unit.GetActiveBuffs()
					.Select(static buff => new BattleBoardBuffVisual(
						buff.Definition.Name,
						buff.Definition.IsDebuff,
						buff.Level,
						buff.RemainingTurns))
					.ToArray()))
			.ToArray());
	}

	private BoardHighlights ResolveBoardHighlights()
	{
		if (_state is null)
		{
			return BoardHighlights.Empty;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		if (actingUnit is null)
		{
			return BoardHighlights.Empty;
		}

		return _uiState.Mode switch
		{
			BattleUiMode.SelectingMove => new BoardHighlights(
				moveTargets: _engine.GetReachablePositions(_state, actingUnit.Id).Keys.ToHashSet()),
			BattleUiMode.SelectingSkillTarget when _uiState.SelectedSkill is { } skill => ResolveSkillHighlights(_state, actingUnit, skill),
			BattleUiMode.SelectingItemTarget => new BoardHighlights(
				itemTargets: ResolveItemTargetPositions(_state, actingUnit)),
			_ => BoardHighlights.Empty,
		};
	}

	private BoardHighlights ResolveSkillHighlights(BattleState state, BattleUnit actingUnit, SkillInstance skill)
	{
		var skillTargets = EnumerateDiamond(actingUnit.Position, skill.CastSize)
			.Where(state.Grid.Contains)
			.ToHashSet();
		var possibleImpacts = new HashSet<GridPosition>();
		foreach (var target in skillTargets)
		{
			foreach (var impactPosition in BattleEngine.GetImpactPositions(
				actingUnit.Position,
				target,
				skill.ImpactType,
				skill.ImpactSize).Where(state.Grid.Contains))
			{
				possibleImpacts.Add(impactPosition);
			}
		}

		var actualImpacts = _hoveredCellPosition is { } hoveredPosition && skillTargets.Contains(hoveredPosition)
			? BattleEngine.GetImpactPositions(
					actingUnit.Position,
					hoveredPosition,
					skill.ImpactType,
					skill.ImpactSize)
				.Where(state.Grid.Contains)
				.ToHashSet()
			: new HashSet<GridPosition>();

		return new BoardHighlights(
			skillTargets: skillTargets,
			skillPossibleImpacts: possibleImpacts,
			skillActualImpacts: actualImpacts);
	}

	private static IReadOnlySet<GridPosition> ResolveItemTargetPositions(BattleState state, BattleUnit actingUnit)
	{
		if (!actingUnit.HasTrait(TraitId.CanUseItemOnAlly))
		{
			return new HashSet<GridPosition> { actingUnit.Position };
		}

		return state.Units
			.Where(unit => unit.IsAlive &&
				unit.Team == actingUnit.Team &&
				unit.Position.ManhattanDistanceTo(actingUnit.Position) <= 2)
			.Select(static unit => unit.Position)
			.ToHashSet();
	}

	private static IEnumerable<GridPosition> EnumerateDiamond(GridPosition center, int radius)
	{
		for (var y = center.Y - radius; y <= center.Y + radius; y++)
		{
			for (var x = center.X - radius; x <= center.X + radius; x++)
			{
				var position = new GridPosition(x, y);
				if (center.ManhattanDistanceTo(position) <= radius)
				{
					yield return position;
				}
			}
		}
	}

	private bool CanClickCell(GridPosition position, BoardHighlights highlights) =>
		highlights.MoveTargets.Contains(position) ||
		highlights.SkillTargets.Contains(position) ||
		highlights.ItemTargets.Contains(position);

	private void OnCellPressed(GridPosition position)
	{
		if (_state is null)
		{
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		if (actingUnit is null)
		{
			return;
		}

		BattleActionResult result;
		switch (_uiState.Mode)
		{
			case BattleUiMode.SelectingMove:
				var moveEventStart = _state.Events.Count;
				result = _engine.MoveTo(_state, actingUnit.Id, position);
				AppendResult(result, moveEventStart);
				SelectDefaultPostMoveMode(actingUnit);
				RefreshAll();
				return;
			case BattleUiMode.SelectingSkillTarget when _uiState.SelectedSkill is { } skill:
				var skillEventStart = _state.Events.Count;
				result = _engine.CastSkill(_state, actingUnit.Id, skill, position);
				AppendResult(result, skillEventStart);
				if (result.Success)
				{
					AudioManager.Instance.PlaySfx(skill.Audio);
					_boardGrid.PlaySkillCast(
						actingUnit.Id,
						result.AffectedUnitIds,
						result.ImpactedPositions,
						skill.Animation);
					AdvanceToNextPlayerAction();
				}
				else
				{
					RefreshAll();
				}
				return;
			case BattleUiMode.SelectingItemTarget when _uiState.SelectedItem is { } item:
				var target = _state.GetUnitAt(position);
				if (target is null)
				{
					return;
				}

				var itemEventStart = _state.Events.Count;
				result = _engine.UseItem(
					_state,
					actingUnit.Id,
					item.Definition,
					target.Id);
				AppendResult(result, itemEventStart);
				if (result.Success)
				{
					GameRoot.InventoryService.RemoveItem(item.Definition);
					AdvanceToNextPlayerAction();
				}
				else
				{
					RefreshAll();
				}
				return;
		}
	}

	private void OnBoardHoveredCellChanged(GridPosition? position)
	{
		var nextHoveredPosition =
			_uiState.Mode == BattleUiMode.SelectingSkillTarget
				? position
				: null;
		if (_hoveredCellPosition == nextHoveredPosition)
		{
			return;
		}

		_hoveredCellPosition = nextHoveredPosition;
		RefreshBoard();
	}

	private void RefreshActions()
	{
		var isActing = _state?.CurrentAction is not null && _uiState.Mode != BattleUiMode.BattleEnded;
		_moveButton.Disabled = !isActing;
		_skillButton.Disabled = !isActing;
		_itemButton.Disabled = !isActing;
		_restButton.Disabled = !isActing;
		_endButton.Disabled = !isActing;
		_finishButton.Visible = _uiState.Mode == BattleUiMode.BattleEnded;
	}

	private void RefreshList()
	{
		ClearChildren(_listContainer);
		if (_state is null)
		{
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		if (actingUnit is null)
		{
			AddListLabel("无可选项");
			return;
		}

		switch (_uiState.Mode)
		{
			case BattleUiMode.SelectingItem:
				AddListLabel("从物品面板中选择消耗品。");
				break;
			case BattleUiMode.SelectingItemTarget:
				AddListLabel(_uiState.SelectedItem is null
					? "点击高亮目标使用。"
					: $"使用：{_uiState.SelectedItem.Definition.Name}");
				AddCancelButton();
				break;
			default:
				foreach (var skillView in _presenter.CreateSkillList(actingUnit))
				{
					AddSkillButton(skillView.Skill, skillView.Label);
				}
				if (_uiState.Mode is BattleUiMode.SelectingMove or BattleUiMode.SelectingSkillTarget)
				{
					AddCancelButton();
				}
				break;
		}
	}

	private void AddListHeader(string text)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", 22);
		label.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.8f));
		_listContainer.AddChild(label);
	}

	private void AddListLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", Colors.White);
		_listContainer.AddChild(label);
	}

	private void AddListButton(string text, Action pressed)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(180, 60),
		};
		button.AddThemeFontSizeOverride("font_size", 18);
		button.Pressed += pressed;
		_listContainer.AddChild(button);
	}

	private void AddSkillButton(SkillInstance skill, string text)
	{
		if (BattleSkillBoxScene.Instantiate() is not BattleSkillBox button)
		{
			throw new InvalidOperationException("BattleSkillBox scene root must be BattleSkillBox.");
		}

		button.Setup(skill, ReferenceEquals(_uiState.SelectedSkill, skill));
		button.TooltipText = text;
		button.Pressed += () =>
		{
			_uiState.SelectSkillTarget(skill);
			RefreshAll();
		};
		_listContainer.AddChild(button);
	}

	private void AddCancelButton() => AddListButton("取消", () =>
	{
		_uiState.ActUnit();
		RefreshAll();
	});

	private async Task OpenItemPanelAsync()
	{
		if (_state is null || BattlePresenter.TryGetActingUnit(_state) is null)
		{
			return;
		}

		var itemEntries = _presenter.CreateItemList(GameRoot.State.Inventory)
			.Select(static view => view.Entry)
			.ToArray();
		if (itemEntries.Length == 0)
		{
			UIRoot.Instance.ShowSuggestion("当前没有可用的消耗品。");
			return;
		}

		if (BattleItemPanelScene is null)
		{
			throw new InvalidOperationException("BattleItemPanelScene is not assigned.");
		}

		_uiState.SelectItemList();
		RefreshAll();

		var instance = BattleItemPanelScene.Instantiate();
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
				RefreshAll();
			}

			return;
		}

		_uiState.SelectItemTarget(selectedEntry);
		RefreshAll();
	}

	private void RefreshAvatar()
	{
		if (_state is null)
		{
			_avatar.Texture = null;
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		_avatar.Texture = actingUnit is null
			? null
			: AssetResolver.LoadCharacterPortrait(actingUnit.Character);
	}

	private void AppendResult(BattleActionResult result, int stateEventStartIndex)
	{
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			AppendLog(result.Message);
		}

		if (!result.Success)
		{
			return;
		}

		var events = _state is not null && stateEventStartIndex >= 0
			? _state.Events.Skip(stateEventStartIndex)
			: result.Events;
		foreach (var battleEvent in events)
		{
			AppendEvent(battleEvent);
		}
	}

	private void AppendEvent(BattleEvent battleEvent)
	{
		if (_state is null)
		{
			return;
		}

		switch (battleEvent.Kind)
		{
			case BattleEventKind.SkillCast:
				var skillName = ResolveSkillName(battleEvent.UnitId, battleEvent.Detail);
				if (!string.IsNullOrWhiteSpace(skillName))
				{
					_boardGrid.PlayFloatText(battleEvent.UnitId, skillName, ResolveSkillColor(battleEvent.UnitId, battleEvent.Detail));
				}
				break;
			case BattleEventKind.Damaged:
				var unitName = _state.TryGetUnit(battleEvent.UnitId)?.Character.Name ?? battleEvent.UnitId;
				var damage = ResolveDamageAmount(battleEvent.Detail);
				_boardGrid.PlayFloatText(
					battleEvent.UnitId,
					damage <= 0 ? "MISS" : $"-{damage}",
					FloatDamageColor);
				AppendLog($"{unitName} 受到 {damage} 点伤害。");
				break;
			case BattleEventKind.BuffApplied:
				var buffName = ResolveBuffName(battleEvent.Detail);
				_boardGrid.PlayFloatText(battleEvent.UnitId, buffName, FloatStateColor);
				AppendLog($"{battleEvent.UnitId} 获得状态 {buffName}。");
				break;
			case BattleEventKind.BuffExpired:
				var expiredBuffName = ResolveBuffName(battleEvent.Detail);
				_boardGrid.PlayFloatText(battleEvent.UnitId, $"{expiredBuffName}解除", FloatInfoColor);
				break;
			case BattleEventKind.Rested:
				_boardGrid.PlayFloatText(battleEvent.UnitId, "回复", FloatHealColor);
				break;
			case BattleEventKind.ItemUsed:
				_boardGrid.PlayFloatText(battleEvent.UnitId, ResolveItemName(battleEvent.Detail), FloatInfoColor);
				break;
			case BattleEventKind.SpeechRequested:
				if (!string.IsNullOrWhiteSpace(battleEvent.Speech?.Text))
				{
					_boardGrid.PlaySpeech(battleEvent.UnitId, battleEvent.Speech.Text);
				}
				break;
		}
	}

	private void AppendLog(string text)
	{
		_logLines.Add(text);
		if (_logLines.Count > 12)
		{
			_logLines.RemoveAt(0);
		}
	}

	private void RefreshLog()
	{
		_logLabel.Text = string.Join('\n', _logLines);
	}

	private SkillInstance? ResolvePreviewSkill(BattleUnit? actingUnit)
	{
		if (actingUnit is null)
		{
			return null;
		}

		return _uiState.SelectedSkill ?? _presenter.CreateSkillList(actingUnit).FirstOrDefault()?.Skill;
	}

	private string ResolveSkillName(string unitId, string? skillId)
	{
		if (string.IsNullOrWhiteSpace(skillId) || _state?.TryGetUnit(unitId) is not { } unit)
		{
			return skillId ?? string.Empty;
		}

		return EnumerateBattleSkills(unit)
			.FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal))
			?.Name ?? skillId;
	}

	private Color ResolveSkillColor(string unitId, string? skillId)
	{
		if (string.IsNullOrWhiteSpace(skillId) || _state?.TryGetUnit(unitId) is not { } unit)
		{
			return FloatInfoColor;
		}

		return EnumerateBattleSkills(unit)
			.FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal))
			?.SkillKind switch
			{
				SkillKind.Special or SkillKind.Legend => FloatSpecialColor,
				SkillKind.Internal => FloatManaColor,
				_ => FloatInfoColor,
			};
	}

	private static IEnumerable<SkillInstance> EnumerateBattleSkills(BattleUnit unit) =>
		unit.Character.GetExternalSkills()
			.Cast<SkillInstance>()
			.Concat(unit.Character.GetInternalSkills())
			.Concat(unit.Character.GetSpecialSkills())
			.Concat(unit.Character.GetFormSkills());

	private static int ResolveDamageAmount(string? detail)
	{
		if (string.IsNullOrWhiteSpace(detail))
		{
			return 0;
		}

		var value = detail.Split(':').LastOrDefault();
		return int.TryParse(value, out var damage) ? damage : 0;
	}

	private static string ResolveBuffName(string? buffId)
	{
		if (string.IsNullOrWhiteSpace(buffId))
		{
			return "状态";
		}

		return GameRoot.ContentRepository.TryGetBuff(buffId, out var definition)
			? definition.Name
			: buffId;
	}

	private static string ResolveItemName(string? itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return "物品";
		}

		return GameRoot.ContentRepository.TryGetItem(itemId, out var definition)
			? definition.Name
			: itemId;
	}

	private void SelectDefaultPostMoveMode(BattleUnit actingUnit)
	{
		var availableSkills = _presenter.CreateSkillList(actingUnit);
		var defaultSkill = availableSkills.FirstOrDefault()?.Skill;
		if (defaultSkill is null)
		{
			_uiState.ActUnit();
			return;
		}

		_uiState.SelectSkillTarget(defaultSkill);
	}

	private static void ApplyLegacySkillName(Label nameLabel, Label formNameLabel, string skillName)
	{
		var segments = skillName.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		if (segments.Length == 2)
		{
			nameLabel.Text = segments[0];
			formNameLabel.Text = segments[1];
			formNameLabel.Visible = true;
			return;
		}

		nameLabel.Text = skillName;
		formNameLabel.Text = string.Empty;
		formNameLabel.Visible = false;
	}

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void FinishBattle()
	{
		if (_state is null)
		{
			QueueFree();
			return;
		}

		var playerAlive = _state.Units.Any(static unit => unit.Team == PlayerTeam && unit.IsAlive);
		if (_battleCompletion.TrySetResult(playerAlive))
		{
			QueueFree();
		}
	}

	private static Color ResolveCellColor(BattleCellView cell, BoardHighlights highlights)
	{
		if (highlights.SkillActualImpacts.Contains(cell.Position))
		{
			return SkillActualImpactColor;
		}

		if (highlights.SkillTargets.Contains(cell.Position))
		{
			return SkillTargetColor;
		}

		if (cell.IsActing)
		{
			return ActingUnitColor;
		}

		if (highlights.MoveTargets.Contains(cell.Position))
		{
			return MoveHighlightColor;
		}

		if (highlights.ItemTargets.Contains(cell.Position))
		{
			return SkillTargetColor;
		}

		if (cell.HasUnit)
		{
			return cell.IsPlayerUnit
				? PlayerUnitColor
				: EnemyUnitColor;
		}

		if (highlights.SkillPossibleImpacts.Contains(cell.Position))
		{
			return SkillPossibleImpactColor;
		}

		return DefaultCellColor;
	}

	private sealed class BoardHighlights
	{
		public static BoardHighlights Empty { get; } = new();

		public BoardHighlights(
			IReadOnlySet<GridPosition>? moveTargets = null,
			IReadOnlySet<GridPosition>? skillTargets = null,
			IReadOnlySet<GridPosition>? skillPossibleImpacts = null,
			IReadOnlySet<GridPosition>? skillActualImpacts = null,
			IReadOnlySet<GridPosition>? itemTargets = null)
		{
			MoveTargets = moveTargets ?? new HashSet<GridPosition>();
			SkillTargets = skillTargets ?? new HashSet<GridPosition>();
			SkillPossibleImpacts = skillPossibleImpacts ?? new HashSet<GridPosition>();
			SkillActualImpacts = skillActualImpacts ?? new HashSet<GridPosition>();
			ItemTargets = itemTargets ?? new HashSet<GridPosition>();
		}

		public IReadOnlySet<GridPosition> MoveTargets { get; }

		public IReadOnlySet<GridPosition> SkillTargets { get; }

		public IReadOnlySet<GridPosition> SkillPossibleImpacts { get; }

		public IReadOnlySet<GridPosition> SkillActualImpacts { get; }

		public IReadOnlySet<GridPosition> ItemTargets { get; }
	}
}
