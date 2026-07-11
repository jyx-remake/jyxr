using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Application;
using Game.Godot.Assets;
using Game.Godot.Audio;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

public partial class BattleScreen : Control
{
	[Export]
	public PackedScene BattleSkillBoxScene { get; set; } = null!;

	[Export]
	public PackedScene BattleItemPanelScene { get; set; } = null!;

	[Export]
	public PackedScene BattleLegendOverlayScene { get; set; } = null!;

	[Export]
	public PackedScene BattleSettlementPanelScene { get; set; } = null!;

	[Export]
	public PackedScene BattleStatusPanelScene { get; set; } = null!;

	private BattlePresenter _presenter = null!;
	private readonly BattleUiStateMachine _uiState = new();

	private BattleDefinition? _battleDefinition;
	private SpecialBattleRequest? _battleRequest;
	private BattleState? _state;
	private BattleFlowOrchestrator? _orchestrator;
	private bool _isConfigured;
	private bool _isResolvingSkillPresentation;
	private BattleSettlementController _settlementController = null!;
	private BattleSettingsController _settingsController = null!;
	private BattleEventPresenter _eventPresenter = null!;
	private BattleSkillPresentationController _skillPresentationController = null!;
	private BattleBoardController _boardController = null!;
	private BattleActionPanelController _actionPanelController = null!;

	private TextureRect _background = null!;
	private Label _titleLabel = null!;
	private Label _subtitleLabel = null!;
	private BaseButton _surrenderButton = null!;
	private BaseButton _speedUpButton = null!;
	private CanvasItem _speedUpActive = null!;
	private BaseButton _autoBattleButton = null!;
	private CanvasItem _autoBattleActive = null!;
	private BattleBoardView _boardGrid = null!;
	private BattleSelectedSkillBox _selectedSkillBox = null!;
	private BaseButton _moveButton = null!;
	private BaseButton _statusButton = null!;
	private BaseButton _itemButton = null!;
	private BaseButton _restButton = null!;
	private BaseButton _endButton = null!;
	private TextureRect _avatar = null!;
	private HBoxContainer _listContainer = null!;
	private RichTextLabel _logLabel = null!;
	private Control _overlayRoot = null!;
	private int PlayerTeam => GameRoot.Config.BattlePlayerTeam;

	public override void _Ready()
	{
		_presenter = new BattlePresenter(PlayerTeam);
		_background = GetNode<TextureRect>("%Background");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_surrenderButton = GetNode<BaseButton>("%SurrenderButton");
		_speedUpButton = GetNode<BaseButton>("%SpeedUpButton");
		_speedUpActive = GetNode<CanvasItem>("%SpeedUpActive");
		_autoBattleButton = GetNode<BaseButton>("%AutoBattleButton");
		_autoBattleActive = GetNode<CanvasItem>("%AutoBattleActive");
		_boardGrid = GetNode<BattleBoardView>("%BoardGrid");
		_selectedSkillBox = GetNode<BattleSelectedSkillBox>("%BattleSelectedSkillBox");
		_moveButton = GetNode<BaseButton>("%MoveButton");
		_statusButton = GetNode<BaseButton>("%StatusButton");
		_itemButton = GetNode<BaseButton>("%ItemButton");
		_restButton = GetNode<BaseButton>("%RestButton");
		_endButton = GetNode<BaseButton>("%EndButton");
		_avatar = GetNode<TextureRect>("%Avatar");
		_listContainer = GetNode<HBoxContainer>("%ListContainer");
		_logLabel = GetNode<RichTextLabel>("%LogLabel");
		_overlayRoot = GetNode<Control>("%OverlayRoot");
		_skillPresentationController = new BattleSkillPresentationController(
			this,
			_boardGrid,
			_overlayRoot,
			BattleLegendOverlayScene,
			() => _eventPresenter,
			value => _isResolvingSkillPresentation = value,
			() => _actionPanelController.RefreshActions(),
			() => RefreshAll());
		_eventPresenter = new BattleEventPresenter(
			_boardGrid,
			_logLabel,
			() => _state,
			_skillPresentationController.Schedule);
		_boardController = new BattleBoardController(
			_boardGrid,
			_presenter,
			_uiState,
			PlayerTeam,
			() => _state,
			() => _orchestrator,
			() => _isResolvingSkillPresentation);
		_actionPanelController = new BattleActionPanelController(
			_presenter,
			_uiState,
			PlayerTeam,
			() => _state,
			() => _orchestrator,
			() => _isResolvingSkillPresentation,
			IsAutoBattleEnabled,
			refreshList => RefreshAll(refreshList),
			new BattleActionPanelView(
				_selectedSkillBox,
				_moveButton,
				_statusButton,
				_itemButton,
				_restButton,
				_endButton,
				_surrenderButton,
				_avatar,
				_listContainer,
				_overlayRoot),
			new BattleActionPanelScenes(
				BattleSkillBoxScene,
				BattleItemPanelScene,
				BattleStatusPanelScene));
		_settingsController = new BattleSettingsController(
			_boardGrid,
			_speedUpActive,
			_autoBattleActive,
			() => _orchestrator,
			IsInsideTree,
			() => _isResolvingSkillPresentation,
			() => _actionPanelController.RefreshActions(),
			AppendLog);
		_settlementController = new BattleSettlementController(
			this,
			BattleSettlementPanelScene,
			_overlayRoot,
			_presenter,
			_uiState.EndBattle,
			AppendLog,
			() => RefreshAll());
		_boardGrid.CellPressed += _boardController.OnCellPressed;
		_boardGrid.HoveredCellChanged += _boardController.OnHoveredCellChanged;
		_surrenderButton.Pressed += SurrenderBattle;
		_speedUpButton.Pressed += _settingsController.ToggleSpeedUp;
		_autoBattleButton.Pressed += async () => await _settingsController.ToggleAutoBattleAsync();
		_actionPanelController.BindButtons();
		_settingsController.RefreshButtons();

		if (_isConfigured)
		{
			StartBattle();
		}
	}

	public async Task<bool> AwaitBattleAsync(CancellationToken cancellationToken = default)
		=> await _settlementController.AwaitAsync(cancellationToken);

	public override void _ExitTree()
	{
		_settingsController.RestoreTimeScale();
		base._ExitTree();
		_settlementController.Cancel();
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
		_settingsController.Load();
		_eventPresenter.Clear();
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

	internal void RefreshAll(bool refreshList = true)
	{
		if (_state is null || !IsInsideTree())
		{
			return;
		}

		var header = _presenter.CreateHeader(_state, _uiState.Mode);
		_titleLabel.Text = _battleDefinition?.Name ?? header.Title;
		_subtitleLabel.Text = header.Subtitle;

		RefreshBoard();
		_actionPanelController.RefreshSelectedSkill();
		_actionPanelController.RefreshActions();
		if (refreshList)
		{
			_actionPanelController.RefreshList();
		}

		_actionPanelController.RefreshAvatar();
		_eventPresenter.Refresh();
		_settingsController.RefreshButtons();
	}

	private void RefreshBoard() => _boardController.Refresh();

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

	internal void AppendResult(BattleCommandResult<BattleActionResult> result) =>
		_eventPresenter.AppendResult(result);

	internal void AppendMessages(IReadOnlyList<BattleMessage> messages) =>
		_eventPresenter.AppendMessages(messages);

	internal void AppendLog(string text) => _eventPresenter.AppendLog(text);
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
		_actionPanelController.SelectDefaultPostMoveMode(actingUnit);
		RefreshAll();
	}

	internal void ShowBattleEnded(bool isWin) =>
		_settlementController.Complete(isWin, _state, _battleRequest);

	internal void ApplyActingUnitFacing(BattleUnit actingUnit) => _boardGrid.ApplyUnitFacing(actingUnit.Id, actingUnit.Facing);

	internal Task PlayMoveAsync(BattleUnit actingUnit, IReadOnlyList<GridPosition> movementPath) =>
		_skillPresentationController.PlayMoveAsync(actingUnit, movementPath);

	internal Task PlaySkillAsync(
		BattleUnit actingUnit,
		SkillInstance skill,
		BattleCommandResult<BattleActionResult> result) =>
		_skillPresentationController.PlaySkillAsync(actingUnit, skill, result);

	private bool IsAutoBattleEnabled() => _orchestrator?.IsAutoBattleEnabled ?? false;

}
