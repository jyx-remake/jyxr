using Game.Core.Affix;
using Game.Core.Battle;
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
	private const double BattleSpeedUpMultiplier = 2d;
	private const int GridCellWidth = 144;
	private const int GridCellHeight = 144;
	private const BattleMovementPresentationMode MovementPresentationMode = BattleMovementPresentationMode.Step;
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
	private const double SkillNameFloatDelaySeconds = 0.1d;
	private const double SkillImpactDelaySeconds = 0.8d;
	private const double SkillImpactFloatDelaySeconds = 0.1d;
	private static readonly string[] LegendEffectSfxIds =
	[
		"音效.奥义1",
		"音效.奥义2",
		"音效.奥义3",
		"音效.奥义4",
		"音效.奥义5",
		"音效.奥义6",
	];
	private static readonly string[] LegendMaleVoiceSfxIds =
	[
		"音效.猛男",
		"音效.男",
		"音效.男2",
		"音效.男3",
		"音效.男4",
		"音效.男5",
		"音效.男-哼",
		"音效.年轻男",
	];
	private static readonly string[] LegendFemaleVoiceSfxIds =
	[
		"音效.女",
		"音效.女2",
		"音效.女3",
		"音效.女4",
		"音效.女的奸笑",
		"音效.敢点老娘",
	];

	[Export]
	public PackedScene BattleSkillBoxScene { get; set; } = null!;

	[Export]
	public PackedScene BattleItemPanelScene { get; set; } = null!;

	[Export]
	public PackedScene BattleLegendOverlayScene { get; set; } = null!;

	private readonly BattlePresenter _presenter = new();
	private readonly BattleUiStateMachine _uiState = new();
	private readonly TaskCompletionSource<bool> _battleCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly List<string> _logLines = [];

	private BattleDefinition? _battleDefinition;
	private IReadOnlyList<string> _selectedCharacterIds = [];
	private BattleState? _state;
	private BattleFlowOrchestrator? _orchestrator;
	private bool _isConfigured;
	private bool _isResolvingSkillPresentation;
	private bool _isSpeedUpEnabled;
	private double _initialTimeScale = 1d;
	private SkillPresentationContext? _activeSkillPresentation;
	private GridPosition? _hoveredCellPosition;

	private TextureRect _background = null!;
	private Label _titleLabel = null!;
	private Label _subtitleLabel = null!;
	private BaseButton _surrenderButton = null!;
	private BaseButton _speedUpButton = null!;
	private CanvasItem _speedUpActive = null!;
	private BaseButton _autoBattleButton = null!;
	private CanvasItem _autoBattleActive = null!;
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
		_initialTimeScale = Engine.TimeScale;
		_background = GetNode<TextureRect>("%Background");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_surrenderButton = GetNode<BaseButton>("%SurrenderButton");
		_speedUpButton = GetNode<BaseButton>("%SpeedUpButton");
		_speedUpActive = GetNode<CanvasItem>("%SpeedUpActive");
		_autoBattleButton = GetNode<BaseButton>("%AutoBattleButton");
		_autoBattleActive = GetNode<CanvasItem>("%AutoBattleActive");
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
		_surrenderButton.Pressed += SurrenderBattle;
		_speedUpButton.Pressed += ToggleSpeedUp;
		_autoBattleButton.Pressed += async () => await ToggleAutoBattleAsync();

		_moveButton.Pressed += async () =>
		{
			if (_orchestrator is null)
			{
				return;
			}

			if (_state is not null &&
				BattlePresenter.TryGetActingUnit(_state) is not null &&
				_state.CurrentAction is { HasMoved: true, HasCommittedMainAction: false })
			{
				await _orchestrator.TryRollbackMoveAsync();
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
		_restButton.Pressed += async () =>
		{
			if (_orchestrator is null)
			{
				return;
			}

			await _orchestrator.TryRestAsync();
		};
		_endButton.Pressed += async () =>
		{
			if (_orchestrator is null)
			{
				return;
			}

			await _orchestrator.TryEndActionAsync();
		};
		_finishButton.Pressed += FinishBattle;
		RefreshToggleButtons();

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
		RestoreTimeScale();
		base._ExitTree();
		if (!_battleCompletion.Task.IsCompleted)
		{
			_battleCompletion.TrySetCanceled();
		}
	}

	private async void StartBattle()
	{
		if (_battleDefinition is null)
		{
			throw new InvalidOperationException("Battle screen has not been configured.");
		}

		ApplyBattlePresentation(_battleDefinition);
		_state = GameRoot.BattleService.BuildBattleState(_battleDefinition, _selectedCharacterIds);
		_orchestrator = new BattleFlowOrchestrator(this, _state);
		_logLines.Clear();
		AppendLog($"战斗开始：{_battleDefinition.Name}");
		_uiState.WaitTimeline();
		RefreshAll();
		await _orchestrator.StartAsync();
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

	internal void RefreshAll()
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
		RefreshToggleButtons();
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
				moveTargets: (_orchestrator?.GetReachablePositions().Keys ?? Array.Empty<GridPosition>()).ToHashSet()),
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

	private async void OnCellPressed(GridPosition position)
	{
		if (_state is null || _orchestrator is null || _isResolvingSkillPresentation)
		{
			return;
		}

		var actingUnit = BattlePresenter.TryGetActingUnit(_state);
		if (actingUnit is null)
		{
			return;
		}

		switch (_uiState.Mode)
		{
			case BattleUiMode.SelectingMove:
				await _orchestrator.TryMoveAsync(position);
				return;
			case BattleUiMode.SelectingSkillTarget when _uiState.SelectedSkill is { } skill:
				await _orchestrator.TryCastSkillAsync(skill, position);
				return;
			case BattleUiMode.SelectingItemTarget when _uiState.SelectedItem is { } item:
				var target = _state.GetUnitAt(position);
				if (target is null)
				{
					return;
				}

				await _orchestrator.TryUseItemAsync(item, target.Id);
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
		var actingUnit = _state is not null ? BattlePresenter.TryGetActingUnit(_state) : null;
		var isActing = actingUnit is { Team: PlayerTeam } &&
			_uiState.Mode != BattleUiMode.BattleEnded &&
			!_isResolvingSkillPresentation &&
			!IsAutoBattleEnabled();
		_moveButton.Disabled = !isActing;
		_skillButton.Disabled = !isActing;
		_itemButton.Disabled = !isActing;
		_restButton.Disabled = !isActing;
		_endButton.Disabled = !isActing;
		_surrenderButton.Disabled = _uiState.Mode == BattleUiMode.BattleEnded;
		_surrenderButton.Visible = _uiState.Mode != BattleUiMode.BattleEnded;
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
		if (actingUnit is null || actingUnit.Team != PlayerTeam)
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
				foreach (var skillView in GetSkillOptions(actingUnit))
				{
					AddSkillButton(skillView);
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

	private void AddSkillButton(BattleSkillOptionView skillView)
	{
		if (BattleSkillBoxScene.Instantiate() is not BattleSkillBox button)
		{
			throw new InvalidOperationException("BattleSkillBox scene root must be BattleSkillBox.");
		}

		button.Setup(skillView.Skill, ReferenceEquals(_uiState.SelectedSkill, skillView.Skill), skillView.IsAvailable);
		button.TooltipText = BuildSkillTooltip(skillView);
		if (skillView.IsAvailable)
		{
			button.Pressed += () =>
			{
				_uiState.SelectSkillTarget(skillView.Skill);
				RefreshAll();
			};
		}

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

	internal void AppendResult(BattleActionResult result, int stateEventStartIndex)
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

		if (TryScheduleSkillPresentationEvent(battleEvent))
		{
			return;
		}

		AppendEventImmediate(battleEvent);
	}

	private bool TryScheduleSkillPresentationEvent(BattleEvent battleEvent)
	{
		if (_activeSkillPresentation is not { } presentation)
		{
			return false;
		}

		switch (battleEvent.Kind)
		{
			case BattleEventKind.SkillCast when
				string.Equals(battleEvent.UnitId, presentation.ActingUnitId, StringComparison.Ordinal) &&
				string.Equals(
					battleEvent.SkillCast?.ResolvedSkillId ?? battleEvent.Detail,
					presentation.SkillCast.ResolvedSkillId,
					StringComparison.Ordinal):
				presentation.EnqueueSkillName(() => AppendEventImmediate(battleEvent));
				return true;
			case BattleEventKind.SpeechRequested:
				presentation.EnqueueImpact(() => AppendEventImmediate(battleEvent));
				return true;
			case BattleEventKind.Damaged:
			case BattleEventKind.BuffApplied:
			case BattleEventKind.BuffExpired:
			case BattleEventKind.Healed:
			case BattleEventKind.MpDamaged:
			case BattleEventKind.Rested:
			case BattleEventKind.ItemUsed:
				presentation.EnqueueImpactFloat(() => AppendEventImmediate(battleEvent));
				return true;
			default:
				return false;
		}
	}

	private void AppendEventImmediate(BattleEvent battleEvent)
	{
		if (_state is null)
		{
			return;
		}

		switch (battleEvent.Kind)
		{
			case BattleEventKind.SkillCast:
				var skillCast = battleEvent.SkillCast;
				if (skillCast is not null)
				{
					var casterName = _state.TryGetUnit(battleEvent.UnitId)?.Character.Name ?? battleEvent.UnitId;
					_boardGrid.PlayFloatText(
						battleEvent.UnitId,
						skillCast.ResolvedSkillName,
						ResolveSkillColor(skillCast));
					AppendLog(skillCast.IsLegend
						? $"{casterName} 施展奥义【{skillCast.ResolvedSkillName}】。"
						: $"{casterName} 施展【{skillCast.ResolvedSkillName}】。");
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

	internal void AppendLog(string text)
	{
		_logLines.Add(text);
		if (_logLines.Count > 12)
		{
			_logLines.RemoveAt(0);
		}

		if (IsNodeReady())
		{
			RefreshLog();
		}
	}

	private void RefreshLog()
	{
		_logLabel.Text = string.Join('\n', _logLines);
	}

	private SkillInstance? ResolvePreviewSkill(BattleUnit? actingUnit)
	{
		var skillOptions = actingUnit is null ? [] : GetSkillOptions(actingUnit);
		if (_uiState.SelectedSkill is { } selectedSkill &&
			skillOptions.Any(skill => skill.IsAvailable && ReferenceEquals(skill.Skill, selectedSkill)))
		{
			return selectedSkill;
		}

		return skillOptions.FirstOrDefault(static skill => skill.IsAvailable)?.Skill;
	}

	private IReadOnlyList<BattleSkillOptionView> GetSkillOptions(BattleUnit actingUnit)
	{
		if (_state is null || _orchestrator is null)
		{
			return [];
		}

		return _presenter.CreateSkillList(_state, _orchestrator.Engine, actingUnit);
	}

	private static string BuildSkillTooltip(BattleSkillOptionView skillView)
	{
		if (skillView.IsAvailable)
		{
			return skillView.Label;
		}

		return $"{skillView.Label}\n{ResolveUnavailableSkillText(skillView.Availability)}";
	}

	private static string ResolveUnavailableSkillText(BattleSkillAvailability availability) =>
		availability.Status switch
		{
			BattleSkillAvailabilityStatus.Cooldown => $"不可用：冷却中（剩余 {availability.RemainingCooldown} 回合）",
			BattleSkillAvailabilityStatus.Disabled => "不可用：当前被封招",
			BattleSkillAvailabilityStatus.NotEnoughMp => "不可用：MP 不足",
			BattleSkillAvailabilityStatus.NotEnoughRage => "不可用：怒气不足",
			_ => "可用",
		};

	private static Color ResolveSkillColor(BattleSkillCastInfo skillCast) =>
		skillCast.IsLegend
			? FloatSpecialColor
			: skillCast.ResolvedSkillKind switch
			{
				SkillKind.Special => FloatSpecialColor,
				SkillKind.Internal => FloatManaColor,
				_ => FloatInfoColor,
			};

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

	private sealed class SkillPresentationContext
	{
		private readonly BattleScreen _owner;
		private readonly BattleBoardView _boardGrid;
		private readonly Control _overlayRoot;
		private readonly string _actingUnitName;
		private readonly CharacterGender _actingUnitGender;
		private readonly Texture2D? _actingUnitPortrait;
		private readonly IReadOnlyList<string> _targetUnitIds;
		private readonly IReadOnlyList<GridPosition> _impactPositions;
		private readonly List<Action> _skillNameActions = [];
		private readonly List<Action> _impactActions = [];
		private readonly List<Action> _impactFloatActions = [];

		public SkillPresentationContext(
			BattleScreen owner,
			BattleBoardView boardGrid,
			Control overlayRoot,
			string actingUnitId,
			string actingUnitName,
			CharacterGender actingUnitGender,
			Texture2D? actingUnitPortrait,
			BattleSkillCastInfo skillCast,
			IReadOnlyList<string> targetUnitIds,
			IReadOnlyList<GridPosition> impactPositions)
		{
			_owner = owner;
			_boardGrid = boardGrid;
			_overlayRoot = overlayRoot;
			ActingUnitId = actingUnitId;
			_actingUnitName = actingUnitName;
			_actingUnitGender = actingUnitGender;
			_actingUnitPortrait = actingUnitPortrait;
			SkillCast = skillCast;
			_targetUnitIds = targetUnitIds;
			_impactPositions = impactPositions;
		}

		public string ActingUnitId { get; }

		public BattleSkillCastInfo SkillCast { get; }

		public void EnqueueSkillName(Action action) => _skillNameActions.Add(action);

		public void EnqueueImpact(Action action) => _impactActions.Add(action);

		public void EnqueueImpactFloat(Action action) => _impactFloatActions.Add(action);

		public async Task RunAsync()
		{
			if (SkillCast.IsLegend)
			{
				PlayLegendIntroSfx(_actingUnitGender);
				if (!string.IsNullOrWhiteSpace(SkillCast.ScreenEffectAnimationId))
				{
					await _owner.ShowLegendOverlayAsync(_overlayRoot, _actingUnitName, _actingUnitPortrait, SkillCast);
				}
			}

			_boardGrid.PlayAttack(ActingUnitId);

			await _owner.WaitForSecondsAsync(SkillNameFloatDelaySeconds);
			Flush(_skillNameActions);

			await _owner.WaitForSecondsAsync(SkillImpactDelaySeconds - SkillNameFloatDelaySeconds);
			if (!string.IsNullOrWhiteSpace(SkillCast.AudioId))
			{
				AudioManager.Instance.PlaySfx(SkillCast.AudioId);
			}

			var impactTask = _boardGrid.PlaySkillImpactAsync(_targetUnitIds, _impactPositions, SkillCast.ImpactAnimationId);
			Flush(_impactActions);

			await _owner.WaitForSecondsAsync(SkillImpactFloatDelaySeconds);
			Flush(_impactFloatActions);
			await impactTask;
		}

		private static void Flush(List<Action> actions)
		{
			foreach (var action in actions)
			{
				action();
			}

			actions.Clear();
		}

		private static void PlayLegendIntroSfx(CharacterGender gender)
		{
			AudioManager.Instance.PlaySfx(PickRandom(gender == CharacterGender.Female
				? LegendFemaleVoiceSfxIds
				: LegendMaleVoiceSfxIds));
			AudioManager.Instance.PlaySfx(PickRandom(LegendEffectSfxIds));
		}

		private static string PickRandom(IReadOnlyList<string> resourceIds) =>
			resourceIds[Random.Shared.Next(resourceIds.Count)];
	}

	private async Task WaitForSecondsAsync(double seconds)
	{
		if (seconds <= 0d)
		{
			return;
		}

		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task ShowLegendOverlayAsync(
		Control overlayRoot,
		string actingUnitName,
		Texture2D? portrait,
		BattleSkillCastInfo skillCast)
	{
		if (BattleLegendOverlayScene is null)
		{
			throw new InvalidOperationException("BattleLegendOverlayScene is not assigned.");
		}

		var instance = BattleLegendOverlayScene.Instantiate();
		if (instance is not BattleLegendOverlay overlay)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle legend overlay scene root must be BattleLegendOverlay.");
		}

		overlayRoot.AddChild(overlay);
		await overlay.PlayAsync(actingUnitName, portrait, skillCast, FloatSpecialColor);
	}

	private async Task PlayMovePresentationAsync(BattleUnit actingUnit, IReadOnlyList<GridPosition> movementPath)
	{
		_isResolvingSkillPresentation = true;
		RefreshActions();
		try
		{
			await _boardGrid.PlayUnitMoveAsync(actingUnit.Id, movementPath, MovementPresentationMode);
		}
		finally
		{
			_isResolvingSkillPresentation = false;
		}

		RefreshAll();
	}

	private async Task PlaySkillPresentationAsync(BattleUnit actingUnit, SkillInstance skill, BattleActionResult result, int stateEventStartIndex)
	{
		_boardGrid.ApplyUnitFacing(actingUnit.Id, actingUnit.Facing);
		_isResolvingSkillPresentation = true;
		RefreshActions();
		_activeSkillPresentation = new SkillPresentationContext(
			this,
			_boardGrid,
			_overlayRoot,
			actingUnit.Id,
			actingUnit.Character.Name,
			actingUnit.Character.Definition.Gender,
			AssetResolver.LoadCharacterPortrait(actingUnit.Character),
			result.SkillCast ?? BattleSkillCastInfo.Create(skill, skill),
			result.AffectedUnitIds,
			result.ImpactedPositions);
		var presentationTask = _activeSkillPresentation.RunAsync();
		AppendResult(result, stateEventStartIndex);
		try
		{
			await presentationTask;
		}
		finally
		{
			_activeSkillPresentation = null;
			_isResolvingSkillPresentation = false;
			RefreshActions();
		}

		RefreshAll();
	}

	private void SelectDefaultPostMoveMode(BattleUnit actingUnit)
	{
		var defaultSkill = GetSkillOptions(actingUnit)
			.FirstOrDefault(static skill => skill.IsAvailable)
			?.Skill;
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

	private void ToggleSpeedUp()
	{
		_isSpeedUpEnabled = !_isSpeedUpEnabled;
		ApplyTimeScale();
		RefreshToggleButtons();
		AppendLog(_isSpeedUpEnabled ? "已开启战斗加速。" : "已关闭战斗加速。");
	}

	private Task ToggleAutoBattleAsync()
	{
		if (_orchestrator is null)
		{
			return Task.CompletedTask;
		}

		var enabled = !_orchestrator.IsAutoBattleEnabled;
		_orchestrator.SetAutoBattleEnabled(enabled);
		AppendLog(enabled ? "已开启自动战斗。" : "已关闭自动战斗。");
		if (IsInsideTree())
		{
			RefreshActions();
			RefreshToggleButtons();
		}

		if (enabled && !_isResolvingSkillPresentation)
		{
			_ = _orchestrator.ContinueBattleFlowAsync();
		}

		return Task.CompletedTask;
	}

	private void RefreshToggleButtons()
	{
		if (!IsInsideTree())
		{
			return;
		}

		_speedUpActive.Visible = _isSpeedUpEnabled;
		_autoBattleActive.Visible = IsAutoBattleEnabled();
	}

	private void ApplyTimeScale()
	{
		Engine.TimeScale = _isSpeedUpEnabled
			? _initialTimeScale * BattleSpeedUpMultiplier
			: _initialTimeScale;
	}

	private void RestoreTimeScale()
	{
		Engine.TimeScale = _initialTimeScale;
	}

	private void SurrenderBattle()
	{
		if (_uiState.Mode == BattleUiMode.BattleEnded || _orchestrator is null)
		{
			return;
		}

		_orchestrator.Surrender();
		RefreshAll();
	}

	internal void BindState(BattleState state)
	{
		_state = state;
		RefreshAll();
	}

	internal void ShowWaitingTimeline()
	{
		_uiState.WaitTimeline();
		RefreshAll();
	}

	internal void ShowPlayerTurn(BattleUnit actingUnit)
	{
		_uiState.SelectMove();
		AppendLog($"轮到 {actingUnit.Character.Name} 行动。");
		RefreshAll();
	}

	internal void ShowPlayerPostMove(BattleUnit actingUnit)
	{
		SelectDefaultPostMoveMode(actingUnit);
		RefreshAll();
	}

	internal void ShowBattleEnded(bool isWin)
	{
		_uiState.EndBattle();
		AppendLog(isWin ? "战斗胜利。" : "战斗失败。");
		RefreshAll();
		_finishButton.Text = isWin ? "胜利返回" : "失败返回";
		_finishButton.Disabled = false;
	}

	internal void ApplyActingUnitFacing(BattleUnit actingUnit) => _boardGrid.ApplyUnitFacing(actingUnit.Id, actingUnit.Facing);

	internal Task PlayMoveAsync(BattleUnit actingUnit, IReadOnlyList<GridPosition> movementPath) =>
		PlayMovePresentationAsync(actingUnit, movementPath);

	internal Task PlaySkillAsync(BattleUnit actingUnit, SkillInstance skill, BattleActionResult result, int stateEventStartIndex) =>
		PlaySkillPresentationAsync(actingUnit, skill, result, stateEventStartIndex);

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

	private bool IsAutoBattleEnabled() => _orchestrator?.IsAutoBattleEnabled ?? false;

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
