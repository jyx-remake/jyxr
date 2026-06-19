using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Application;
using Game.Godot.Assets;
using Game.Godot.Audio;
using Game.Godot.Persistence;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

public partial class BattleScreen : Control
{
	private const int MinBattleSpeedMultiplier = 1;
	private const int MaxBattleSpeedMultiplier = 5;
	private const float DesignWidth = 1920f;
	private const float DesignHeight = 1080f;
	private const float BoardLeftDesign = 215f;
	private const float BoardTopDesign = 190f;
	private const float BoardMinWidth = 560f;
	private const float BoardMinHeight = 300f;
	private const float BoardSafeMarginDesign = 6f;
	private const float TopClockDesignLeft = 22f;
	private const float TopClockDesignTop = 16f;
	private const float TopClockDesignWidth = 814f;
	private const float TopClockDesignHeight = 183f;
	private const float TopButtonGroupDesignWidth = 543f;
	private const float TopButtonRightMarginDesign = 14f;
	private const float TopHudGapDesign = 24f;
	private const float TopHudMinScale = 0.33f;
	private const float TopHudMaxScale = 1f;
	private const float ActionMenuDesignWidth = 520f;
	private const float ActionMenuDesignHeight = 560f;
	private const float ActionButtonDesignWidth = 177f;
	private const float ActionButtonDesignHeight = 160f;
	private const float ActionButtonMinHitSize = 44f;
	private const float ActionMenuMaxScale = 1.02f;
	private const float ActionMenuRightMarginDesign = 15f;
	private const float ActionMenuBottomMarginDesign = 0f;
	private const float ActionMenuScreenMarginMin = 8f;
	private const float BottomHudDesignHeight = 260f;
	private const float BottomHudMinHeight = 150f;
	private const float BottomHudMaxHeight = 260f;
	private const float SelectedSkillDesignLeft = 55f;
	private const float SelectedSkillDesignWidth = 257f;
	private const float SelectedSkillDesignHeight = 282f;
	private const float SkillListMinWidth = 220f;
	private const float SkillListMinScreenWidth = 180f;
	private const float SkillListMinHeight = 96f;
	private const float SkillListSelectedOverlapDesign = 45f;
	private const float SkillListTopInsetDesign = 88f;
	private const float SkillListRightGapDesign = 34f;
	private const float BottomStripDesignLeft = 20f;
	private const float BottomStripDesignHeight = 163f;
	private const float BottomStripTopInsetDesign = 46f;
	private const float SkillListItemMinScale = 0.45f;
	private const float SkillListItemMaxScale = 1f;
	private const float ListButtonDesignWidth = 180f;
	private const float ListButtonDesignHeight = 60f;
	private const int ListHeaderDesignFontSize = 22;
	private const int ListLabelDesignFontSize = 18;
	private const int ListButtonDesignFontSize = 18;
	private const int ListSeparationDesign = 12;
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
	private const string RestSfxId = "音效.休息";
	private const double SkillNameFloatDelaySeconds = 0.1d;
	private const double SkillImpactDelaySeconds = 0.8d;
	private const double SkillImpactFloatDelaySeconds = 0.1d;
	[Export]
	public PackedScene BattleSkillBoxScene { get; set; } = null!;

	[Export]
	public PackedScene BattleItemPanelScene { get; set; } = null!;

	[Export]
	public PackedScene BattleLegendOverlayScene { get; set; } = null!;

	[Export]
	public PackedScene BattleSettlementPanelScene { get; set; } = null!;

	private BattlePresenter _presenter = null!;
	private readonly BattleUiStateMachine _uiState = new();
	private readonly LocalUserSettingsStore _settingsStore = new();
	private readonly TaskCompletionSource<bool> _battleCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly List<string> _logLines = [];

	private BattleDefinition? _battleDefinition;
	private SpecialBattleRequest? _battleRequest;
	private BattleState? _state;
	private BattleFlowOrchestrator? _orchestrator;
	private bool _isConfigured;
	private bool _isResolvingSkillPresentation;
	private bool _isEndingBattle;
	private bool _isSpeedUpEnabled;
	private bool _isBattleInfoCollapsed;
	private double _initialTimeScale = 1d;
	private int _battleSpeedMultiplier = 2;
	private float _skillListItemScale = 1f;
	private SkillPresentationContext? _activeSkillPresentation;
	private GridPosition? _hoveredCellPosition;

	private TextureRect _background = null!;
	private TextureRect _topClock = null!;
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
	private TextureRect _avatarFrame = null!;
	private HBoxContainer _listContainer = null!;
	private Control _bottomHud = null!;
	private TextureRect _bottomStrip = null!;
	private TextureRect _actionBarBg = null!;
	private TextureRect _selectedSkillBg = null!;
	private ScrollContainer _listScroll = null!;
	private Control _uiDesignCanvas = null!;
	private Control _uiDesignRoot = null!;
	private Control _boardDesignCanvas = null!;
	private Control _boardDesignRoot = null!;
	private TextureRect _battleLogTag = null!;
	private PanelContainer _logPanel = null!;
	private RichTextLabel _logLabel = null!;
	private Control _overlayRoot = null!;
	private int PlayerTeam => GameRoot.Config.BattlePlayerTeam;

	private readonly record struct ActionMenuLayout(
		float Scale,
		Vector2 Origin,
		Rect2 Bounds,
		Rect2 BackgroundBounds);

	private readonly record struct BottomSkillLayout(
		float SelectedSkillScale,
		Rect2 SelectedSkillBounds,
		Rect2 SkillListBounds,
		Rect2 BottomStripBounds);

	private readonly record struct TopHudLayout(
		float Scale,
		Rect2 Bounds,
		Rect2 ClockBounds,
		Rect2 SurrenderBounds,
		Rect2 AutoBattleBounds,
		Rect2 SpeedUpBounds);

	public override void _Ready()
	{
		_initialTimeScale = Engine.TimeScale;
		_presenter = new BattlePresenter(PlayerTeam);
		_background = GetNode<TextureRect>("%Background");
		_topClock = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/TopClock");
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
		_avatarFrame = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/BottomHud/AvatarFrame");
		_bottomHud = GetNode<Control>("UiDesignCanvas/DesignRoot/BottomHud");
		_bottomStrip = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/BottomHud/BottomStrip");
		_actionBarBg = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/BottomHud/ActionBarBg");
		_selectedSkillBg = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/BottomHud/SelectedSkillBg");
		_listScroll = GetNode<ScrollContainer>("UiDesignCanvas/DesignRoot/BottomHud/ListScroll");
		_uiDesignCanvas = GetNode<Control>("UiDesignCanvas");
		_uiDesignRoot = GetNode<Control>("UiDesignCanvas/DesignRoot");
		_boardDesignCanvas = GetNode<Control>("BoardDesignCanvas");
		_boardDesignRoot = GetNode<Control>("BoardDesignCanvas/DesignRoot");
		_battleLogTag = GetNode<TextureRect>("UiDesignCanvas/DesignRoot/BattleLogTag");
		_logPanel = GetNode<PanelContainer>("UiDesignCanvas/DesignRoot/BattleLogTag/LogPanel");
		_listContainer = GetNode<HBoxContainer>("%ListContainer");
		_logLabel = GetNode<RichTextLabel>("%LogLabel");
		_overlayRoot = GetNode<Control>("%OverlayRoot");
		_boardGrid.CellPressed += OnCellPressed;
		_boardGrid.HoveredCellChanged += OnBoardHoveredCellChanged;
		_surrenderButton.Pressed += SurrenderBattle;
		_speedUpButton.Pressed += ToggleSpeedUp;
		_autoBattleButton.Pressed += async () => await ToggleAutoBattleAsync();
		_battleLogTag.GuiInput += OnBattleLogTagGuiInput;
		_battleLogTag.MouseFilter = MouseFilterEnum.Stop;
		_logPanel.MouseFilter = MouseFilterEnum.Pass;
		ApplyBattleInfoVisibility();
		ConfigureTopActionButtonInput();

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
		RefreshToggleButtons();
		ApplyResponsiveLayout();

		if (_isConfigured)
		{
			StartBattle();
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized)
		{
			ApplyResponsiveLayout();
		}
	}

	public void Configure(string battleId, IReadOnlyList<string> selectedCharacterIds)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
		ArgumentNullException.ThrowIfNull(selectedCharacterIds);

		Configure(new OrdinaryBattleRequest(battleId, selectedCharacterIds.ToArray()));
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
		if (_battleDefinition is null || _battleRequest is null)
		{
			throw new InvalidOperationException("Battle screen has not been configured.");
		}

		ApplyBattlePresentation(_battleDefinition);
		_state = GameRoot.BattleService.BuildBattleState(_battleRequest);
		_orchestrator = new BattleFlowOrchestrator(this, _state);
		ApplyBattleSettings(_settingsStore.LoadOrDefault());
		_logLines.Clear();
		_isEndingBattle = false;
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

	private void ApplyResponsiveLayout()
	{
		if (!IsNodeReady() || Size.X <= 0f || Size.Y <= 0f)
		{
			return;
		}

		FillRoot(_uiDesignCanvas);
		FillRoot(_uiDesignRoot);
		FillRoot(_boardDesignCanvas);
		FillRoot(_boardDesignRoot);

		var scale = ResolveViewportScale();
		var bottomHudHeight = Mathf.Clamp(BottomHudDesignHeight * scale, BottomHudMinHeight, BottomHudMaxHeight);
		var actionMenuLayout = ResolveActionMenuLayout(scale, bottomHudHeight);
		var safeMargin = MathF.Max(12f, BoardSafeMarginDesign * scale);

		var topHudLayout = ResolveTopHudLayout(scale);
		ApplyTopHudLayout(topHudLayout);
		var logPanelRect = new Rect2(Vector2.Zero, Vector2.Zero);

		_bottomHud.Position = Vector2.Zero;
		_bottomHud.Size = Size;

		var actionMenuRect = ApplyActionMenuLayout(actionMenuLayout);
		var bottomSkillLayout = ResolveBottomSkillLayout(scale, bottomHudHeight, actionMenuLayout);
		ApplyBottomSkillLayout(bottomSkillLayout);

		ApplyBoardLayout(
			topHudLayout.Bounds,
			logPanelRect,
			actionMenuRect,
			bottomSkillLayout.SelectedSkillBounds,
			bottomSkillLayout.SkillListBounds,
			safeMargin,
			scale);
	}

	private TopHudLayout ResolveTopHudLayout(float viewportScale)
	{
		var fitDesignWidth =
			TopClockDesignLeft +
			TopClockDesignWidth +
			TopHudGapDesign +
			TopButtonGroupDesignWidth +
			TopButtonRightMarginDesign;
		var fitScale = MathF.Min(TopHudMaxScale, Size.X / fitDesignWidth);
		var minScale = MathF.Min(TopHudMinScale, fitScale);
		var scale = Mathf.Clamp(viewportScale, minScale, MathF.Min(TopHudMaxScale, fitScale));
		var clockBounds = ScaleDesignRect(
			Vector2.Zero,
			scale,
			new Rect2(
				TopClockDesignLeft,
				TopClockDesignTop,
				TopClockDesignWidth,
				TopClockDesignHeight));
		var topButtonOrigin = new Vector2(
			MathF.Max(
				clockBounds.Position.X + clockBounds.Size.X + TopHudGapDesign * scale,
				Size.X - TopButtonGroupDesignWidth * scale - TopButtonRightMarginDesign * scale),
			0f);
		var surrenderBounds = ScaleDesignRect(topButtonOrigin, scale, new Rect2(0f, 29f, 187f, 175f));
		var autoBattleBounds = ScaleDesignRect(topButtonOrigin, scale, new Rect2(191f, 23f, 185f, 158f));
		var speedUpBounds = ScaleDesignRect(topButtonOrigin, scale, new Rect2(385f, 30f, 158f, 136f));
		var bounds = UnionRects(
			UnionRects(clockBounds, surrenderBounds),
			UnionRects(autoBattleBounds, speedUpBounds));

		return new TopHudLayout(scale, bounds, clockBounds, surrenderBounds, autoBattleBounds, speedUpBounds);
	}

	private void ApplyTopHudLayout(TopHudLayout layout)
	{
		ApplyResolvedRect(_topClock, layout.ClockBounds, layout.Scale);
		ApplyResolvedRect(_surrenderButton, layout.SurrenderBounds, layout.Scale);
		ApplyResolvedRect(_autoBattleButton, layout.AutoBattleBounds, layout.Scale);
		ApplyResolvedRect(_speedUpButton, layout.SpeedUpBounds, layout.Scale);
		_surrenderButton.MoveToFront();
		_autoBattleButton.MoveToFront();
		_speedUpButton.MoveToFront();
	}

	private void ConfigureTopActionButtonInput()
	{
		ConfigureTopActionButtonInput(_surrenderButton);
		ConfigureTopActionButtonInput(_autoBattleButton);
		ConfigureTopActionButtonInput(_speedUpButton);
	}

	private static void ConfigureTopActionButtonInput(Control button)
	{
		button.MouseFilter = MouseFilterEnum.Stop;
		button.ZIndex = 80;
	}

	private void ApplyBattleInfoVisibility()
	{
		_logPanel.Visible = !_isBattleInfoCollapsed;
		_battleLogTag.Visible = true;
		_battleLogTag.TooltipText = _isBattleInfoCollapsed ? "展开战斗详情" : "收起战斗详情";
	}

	private void OnBattleLogTagGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			ToggleBattleInfoPanel();
		}
	}

	private void ToggleBattleInfoPanel()
	{
		_isBattleInfoCollapsed = !_isBattleInfoCollapsed;
		ApplyBattleInfoVisibility();
	}

	private BottomSkillLayout ResolveBottomSkillLayout(
		float viewportScale,
		float bottomHudHeight,
		ActionMenuLayout actionMenuLayout)
	{
		var screenMargin = MathF.Max(ActionMenuScreenMarginMin, BoardSafeMarginDesign * viewportScale * 0.5f);
		var selectedScale = actionMenuLayout.Scale;
		var selectedSize = new Vector2(SelectedSkillDesignWidth, SelectedSkillDesignHeight) * selectedScale;
		var selectedLeft = MathF.Max(screenMargin, SelectedSkillDesignLeft * viewportScale);
		var selectedTop = MathF.Min(
			Size.Y - selectedSize.Y - screenMargin,
			Size.Y - bottomHudHeight - 10f * viewportScale);
		selectedTop = MathF.Max(screenMargin, selectedTop);
		var selectedBounds = new Rect2(new Vector2(selectedLeft, selectedTop), selectedSize);

		var listLeft = selectedBounds.Position.X + selectedBounds.Size.X - SkillListSelectedOverlapDesign * viewportScale;
		var listRight = actionMenuLayout.Origin.X - SkillListRightGapDesign * viewportScale;
		var listTop = selectedBounds.Position.Y + SkillListTopInsetDesign * selectedScale;
		var listBottom = Size.Y - screenMargin;
		var minListWidth = MathF.Max(SkillListMinScreenWidth, SkillListMinWidth * viewportScale);
		var minListHeight = SkillListMinHeight;
		if (listRight - listLeft < minListWidth)
		{
			var rightLimit = MathF.Max(screenMargin, actionMenuLayout.Origin.X - screenMargin);
			listLeft = MathF.Max(screenMargin, MathF.Min(listLeft, rightLimit - minListWidth));
			listRight = rightLimit;
		}

		var listBounds = new Rect2(
			new Vector2(listLeft, listTop),
			new Vector2(MathF.Max(1f, listRight - listLeft), MathF.Max(minListHeight, listBottom - listTop)));

		var stripLeft = BottomStripDesignLeft * viewportScale;
		var stripTop = Size.Y - bottomHudHeight + BottomStripTopInsetDesign * viewportScale;
		var stripRight = MathF.Max(stripLeft, actionMenuLayout.Origin.X - SkillListRightGapDesign * viewportScale);
		var stripBounds = new Rect2(
			new Vector2(stripLeft, stripTop),
			new Vector2(stripRight - stripLeft, BottomStripDesignHeight * viewportScale));

		return new BottomSkillLayout(selectedScale, selectedBounds, listBounds, stripBounds);
	}

	private void ApplyBottomSkillLayout(BottomSkillLayout layout)
	{
		_selectedSkillBg.Position = layout.SelectedSkillBounds.Position;
		_selectedSkillBg.Size = new Vector2(SelectedSkillDesignWidth, SelectedSkillDesignHeight);
		_selectedSkillBg.Scale = Vector2.One * layout.SelectedSkillScale;
		_listScroll.Position = layout.SkillListBounds.Position;
		_listScroll.Size = layout.SkillListBounds.Size;
		_skillListItemScale = ResolveSkillListItemScale(layout.SkillListBounds.Size.Y);
		ApplySkillListItemScale();
		_bottomStrip.Position = layout.BottomStripBounds.Position;
		_bottomStrip.Size = layout.BottomStripBounds.Size;
	}

	private ActionMenuLayout ResolveActionMenuLayout(float viewportScale, float bottomHudHeight)
	{
		var minHitScale = MathF.Max(
			ActionButtonMinHitSize / ActionButtonDesignWidth,
			ActionButtonMinHitSize / ActionButtonDesignHeight);
		var rightMargin = MathF.Max(ActionMenuScreenMarginMin, ActionMenuRightMarginDesign * viewportScale);
		var bottomMargin = MathF.Max(0f, ActionMenuBottomMarginDesign * viewportScale);
		var widthScale = Size.X / DesignWidth;
		var heightScale = Size.Y / DesignHeight;
		var availableWidthScale = MathF.Max(minHitScale, (Size.X - rightMargin - ActionMenuScreenMarginMin) / ActionMenuDesignWidth);
		var availableHeightScale = MathF.Max(minHitScale, (Size.Y - bottomMargin - ActionMenuScreenMarginMin) / ActionMenuDesignHeight);
		var maxFitScale = MathF.Min(availableWidthScale, availableHeightScale);
		var targetScale = MathF.Min(MathF.Max(viewportScale, MathF.Min(widthScale, heightScale)), maxFitScale);
		if (bottomHudHeight < BottomHudDesignHeight)
		{
			targetScale = MathF.Min(targetScale, bottomHudHeight / BottomHudDesignHeight);
		}

		var scale = Mathf.Clamp(targetScale, minHitScale, MathF.Min(ActionMenuMaxScale, maxFitScale));
		var size = new Vector2(ActionMenuDesignWidth, ActionMenuDesignHeight) * scale;
		var origin = new Vector2(
			MathF.Max(ActionMenuScreenMarginMin, Size.X - size.X - rightMargin),
			MathF.Max(ActionMenuScreenMarginMin, Size.Y - size.Y - bottomMargin));
		var backgroundOrigin = new Vector2(
			MathF.Max(0f, origin.X - 70f * viewportScale),
			MathF.Max(0f, origin.Y - 8f * viewportScale));
		var backgroundSize = new Vector2(size.X + 70f * viewportScale, size.Y + 14f * viewportScale);

		return new ActionMenuLayout(
			scale,
			origin,
			new Rect2(origin, size),
			new Rect2(backgroundOrigin, backgroundSize));
	}

	private Rect2 ApplyActionMenuLayout(ActionMenuLayout layout)
	{
		_actionBarBg.Position = layout.BackgroundBounds.Position;
		_actionBarBg.Size = layout.BackgroundBounds.Size;
		ApplyScaledRect(_skillButton, layout.Origin, layout.Scale, new Rect2(133f, 152f, 177f, 160f));
		ApplyScaledRect(_itemButton, layout.Origin, layout.Scale, new Rect2(262f, 230f, 177f, 160f));
		ApplyScaledRect(_restButton, layout.Origin, layout.Scale, new Rect2(267f, 385f, 177f, 160f));
		ApplyScaledRect(_moveButton, layout.Origin, layout.Scale, new Rect2(0f, 386f, 177f, 160f));
		ApplyScaledRect(_avatar, layout.Origin, layout.Scale, new Rect2(136f, 294f, 157f, 167f));
		ApplyScaledRect(_avatarFrame, layout.Origin, layout.Scale, new Rect2(121f, 306f, 198f, 164f));
		ApplyScaledRect(_endButton, layout.Origin, layout.Scale, new Rect2(245f, 0f, 202f, 62f));
		return layout.Bounds;
	}

	private void ApplyBoardLayout(
		Rect2 topHudRect,
		Rect2 logPanelRect,
		Rect2 actionMenuRect,
		Rect2 selectedSkillRect,
		Rect2 listScrollRect,
		float safeMargin,
		float scale)
	{
		var minBoardWidth = BoardMinWidth * scale;
		var minBoardHeight = BoardMinHeight * scale;
		var preferredLeft = Mathf.Min(BoardLeftDesign * scale, Size.X * 0.22f);
		var preferredTop = Mathf.Min(BoardTopDesign * scale, Size.Y * 0.26f);
		var leftEdge = MathF.Max(preferredLeft, logPanelRect.Position.X + logPanelRect.Size.X + safeMargin);
		var topEdge = MathF.Max(preferredTop, topHudRect.Position.Y + topHudRect.Size.Y + safeMargin);
		var bottomHudTop = MathF.Min(selectedSkillRect.Position.Y, listScrollRect.Position.Y);
		var rightEdge = Size.X - safeMargin;
		var bottomEdge = MathF.Min(Size.Y - safeMargin, bottomHudTop + BottomStripTopInsetDesign * scale);

		if (rightEdge - leftEdge < minBoardWidth)
		{
			rightEdge = MathF.Min(Size.X - safeMargin, leftEdge + minBoardWidth);
		}

		if (bottomEdge - topEdge < minBoardHeight)
		{
			bottomEdge = MathF.Min(Size.Y - safeMargin, topEdge + minBoardHeight);
		}

		_boardGrid.Position = new Vector2(leftEdge, topEdge);
		_boardGrid.Size = new Vector2(
			MathF.Max(1f, rightEdge - leftEdge),
			MathF.Max(1f, bottomEdge - topEdge));
		_boardGrid.RefreshLayout();
	}

	private float ResolveViewportScale() =>
		MathF.Min(Size.X / DesignWidth, Size.Y / DesignHeight);

	private static void FillRoot(Control control)
	{
		control.Position = Vector2.Zero;
		control.Size = control.GetParent<Control>()?.Size ?? control.Size;
		control.Scale = Vector2.One;
	}

	private float ResolveSkillListItemScale(float listHeight)
	{
		var availableHeight = MathF.Max(1f, listHeight - 8f);
		var heightScale = availableHeight / BattleSkillBox.DesignHeight;
		return Mathf.Clamp(heightScale, SkillListItemMinScale, SkillListItemMaxScale);
	}

	private void ApplySkillListItemScale()
	{
		if (_listContainer is null)
		{
			return;
		}

		var separation = Math.Max(6, (int)MathF.Round(ListSeparationDesign * _skillListItemScale));
		_listContainer.AddThemeConstantOverride("separation", separation);
		foreach (var child in _listContainer.GetChildren())
		{
			if (child is Control control)
			{
				ApplyListItemScale(control);
			}
		}
	}

	private void ApplyListItemScale(Control control)
	{
		if (control is BattleSkillBox skillBox)
		{
			skillBox.SetPresentationScale(_skillListItemScale);
			return;
		}

		if (control is Button button)
		{
			button.CustomMinimumSize = new Vector2(ListButtonDesignWidth, ListButtonDesignHeight) * _skillListItemScale;
			button.AddThemeFontSizeOverride(
				"font_size",
				Math.Max(12, (int)MathF.Round(ListButtonDesignFontSize * _skillListItemScale)));
			return;
		}

		if (control is Label label)
		{
			ApplyListLabelScale(label, ListLabelDesignFontSize);
		}
	}

	private void ApplyListLabelScale(Label label, int designFontSize)
	{
		label.AddThemeFontSizeOverride(
			"font_size",
			Math.Max(12, (int)MathF.Round(designFontSize * _skillListItemScale)));
	}

	private static Rect2 ScaleDesignRect(Vector2 origin, float scale, Rect2 designRect) =>
		new(origin + designRect.Position * scale, designRect.Size * scale);

	private static void ApplyResolvedRect(Control control, Rect2 bounds, float scale)
	{
		control.Position = bounds.Position;
		control.Size = bounds.Size / scale;
		control.Scale = Vector2.One * scale;
	}

	private static Rect2 ApplyScaledRect(Control control, Vector2 origin, float scale, Rect2 designRect)
	{
		control.Position = origin + designRect.Position * scale;
		control.Size = designRect.Size;
		control.Scale = Vector2.One * scale;
		return new Rect2(control.Position, designRect.Size * scale);
	}

	private static Rect2 UnionRects(Rect2 first, Rect2 second)
	{
		var left = MathF.Min(first.Position.X, second.Position.X);
		var top = MathF.Min(first.Position.Y, second.Position.Y);
		var right = MathF.Max(first.Position.X + first.Size.X, second.Position.X + second.Size.X);
		var bottom = MathF.Max(first.Position.Y + first.Size.Y, second.Position.Y + second.Size.Y);
		return new Rect2(left, top, right - left, bottom - top);
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
		_boardGrid.RenderGrid(_state.Grid.Width, _state.Grid.Height, 6, cells);
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

	public void Configure(SpecialBattleRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentException.ThrowIfNullOrWhiteSpace(request.BattleId);
		ArgumentNullException.ThrowIfNull(request.SelectedCharacterIds);

		_battleDefinition = GameRoot.ContentRepository.GetBattle(request.BattleId);
		_battleRequest = request;
		_isConfigured = true;

		if (IsInsideTree())
		{
			StartBattle();
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
		var isActing = actingUnit is not null &&
			actingUnit.Team == PlayerTeam &&
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
		label.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.8f));
		ApplyListLabelScale(label, ListHeaderDesignFontSize);
		_listContainer.AddChild(label);
	}

	private void AddListLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		label.AddThemeColorOverride("font_color", Colors.White);
		ApplyListLabelScale(label, ListLabelDesignFontSize);
		_listContainer.AddChild(label);
	}

	private void AddListButton(string text, Action pressed)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(ListButtonDesignWidth, ListButtonDesignHeight),
		};
		button.AddThemeFontSizeOverride("font_size", 18);
		button.Pressed += pressed;
		ApplyListItemScale(button);
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
		button.SetPresentationScale(_skillListItemScale);
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
			case BattleEventKind.BuffRemoved:
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
				var damage = battleEvent.Damage?.Amount ?? 0;
				var isCritical = damage > 0 && battleEvent.Damage?.IsCritical == true;
				if (damage > 0 &&
					!string.Equals(battleEvent.Damage?.SourceUnitId, battleEvent.UnitId, StringComparison.Ordinal))
				{
					_boardGrid.PlayHit(battleEvent.UnitId);
				}
				_boardGrid.PlayFloatText(
					battleEvent.UnitId,
					ResolveDamageFloatText(damage, isCritical),
					isCritical ? FloatCriticalColor : FloatDamageColor);
				AppendLog(isCritical
					? $"暴击！！{unitName} 受到 {damage} 点伤害。"
					: $"{unitName} 受到 {damage} 点伤害。");
				break;
			case BattleEventKind.BuffApplied:
				var buffName = ResolveBuffName(battleEvent.Detail);
				_boardGrid.PlayFloatText(battleEvent.UnitId, buffName, FloatStateColor);
				AppendLog($"{battleEvent.UnitId} 获得状态 {buffName}。");
				break;
			case BattleEventKind.BuffRemoved:
				var expiredBuffName = ResolveBuffName(battleEvent.Detail);
				_boardGrid.PlayFloatText(battleEvent.UnitId, $"{expiredBuffName}解除", FloatInfoColor);
				break;
			case BattleEventKind.Rested:
				AppendRestEvent(battleEvent);
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

	private void AppendRestEvent(BattleEvent battleEvent)
	{
		var unitName = _state?.TryGetUnit(battleEvent.UnitId)?.Character.Name ?? battleEvent.UnitId;
		var hp = battleEvent.Rest?.Hp ?? 0;
		var mp = battleEvent.Rest?.Mp ?? 0;

		AppendLog($"{unitName}休息。");
		if (hp > 0)
		{
			_boardGrid.PlayFloatText(battleEvent.UnitId, $"+{hp}", FloatHealColor);
			AppendLog($"{unitName}回复生命值{hp}");
		}

		if (mp > 0)
		{
			_boardGrid.PlayFloatText(battleEvent.UnitId, $"+{mp}", FloatManaColor);
			AppendLog($"{unitName}回复内力{mp}");
		}

		if (hp > 0 || mp > 0)
		{
			AudioManager.Instance.PlaySfx(RestSfxId);
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

	private static string ResolveDamageFloatText(int damage, bool isCritical) =>
		damage <= 0
			? "MISS"
			: isCritical ? $"暴击 -{damage}" : $"-{damage}";

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

			var impactTask = _boardGrid.PlaySkillImpactAsync(_impactPositions, SkillCast.ImpactAnimationId);
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
				? GameRoot.Config.LegendFemaleVoiceSfxIds
				: GameRoot.Config.LegendMaleVoiceSfxIds));
			AudioManager.Instance.PlaySfx(PickRandom(GameRoot.Config.LegendEffectSfxIds));
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
		SaveBattleSettings();
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
		SaveBattleSettings();
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
			? _initialTimeScale * _battleSpeedMultiplier
			: _initialTimeScale;
	}

	private void ApplyBattleSettings(UserSettingsRecord settings)
	{
		_isSpeedUpEnabled = settings.BattleSpeedUp;
		_battleSpeedMultiplier = ClampBattleSpeedMultiplier(settings.BattleSpeedMultiplier);
		_orchestrator?.SetAutoBattleEnabled(settings.AutoBattle);
		ApplyTimeScale();
	}

	private void SaveBattleSettings()
	{
		var settings = _settingsStore.LoadOrDefault();
		_settingsStore.Save(settings with
		{
			AutoBattle = IsAutoBattleEnabled(),
			BattleSpeedUp = _isSpeedUpEnabled,
			BattleSpeedMultiplier = _battleSpeedMultiplier,
		});
	}

	private static int ClampBattleSpeedMultiplier(int multiplier) =>
		Math.Clamp(multiplier, MinBattleSpeedMultiplier, MaxBattleSpeedMultiplier);

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

	internal async void ShowBattleEnded(bool isWin)
	{
		if (_isEndingBattle)
		{
			return;
		}

		_isEndingBattle = true;
		_uiState.EndBattle();
		AppendLog(isWin ? "战斗胜利。" : "战斗失败。");
		OrdinaryBattleVictorySettlement? settlement = null;
		if (isWin && _state is not null && _battleRequest is not null)
		{
			settlement = GameRoot.BattleService.PreviewVictorySettlement(_state, _battleRequest);
			GameRoot.BattleService.ApplyOrdinaryVictorySettlement(_state, settlement);
		}

		RefreshAll();
		try
		{
			await ShowSettlementPanelAsync(isWin, settlement);
		}
		finally
		{
			FinishBattle();
		}
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

		var playerAlive = _state.Units.Any(unit => unit.Team == PlayerTeam && unit.IsAlive);
		if (_battleCompletion.TrySetResult(playerAlive))
		{
			QueueFree();
		}
	}

	private async Task ShowSettlementPanelAsync(bool isWin, OrdinaryBattleVictorySettlement? settlement)
	{
		if (BattleSettlementPanelScene is null)
		{
			throw new InvalidOperationException("BattleSettlementPanelScene is not assigned.");
		}

		var instance = BattleSettlementPanelScene.Instantiate();
		if (instance is not BattleSettlementPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle settlement panel scene root must be BattleSettlementPanel.");
		}

		panel.Configure(_presenter.CreateSettlementView(isWin, settlement));
		_overlayRoot.AddChild(panel);
		await panel.AwaitConfirmationAsync();
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
