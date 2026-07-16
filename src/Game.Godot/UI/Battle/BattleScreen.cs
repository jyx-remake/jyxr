using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Presentation.Battle;
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

	private BattleDefinition? _battleDefinition;
	private SpecialBattleRequest? _battleRequest;
	private BattleState? _state;
	private BattleFlowContext? _flowContext;
	private BattleFlowStateMachine? _flow;
	private BattleInteractionState? _interaction;
	private bool _isConfigured;
	private Task _startTask = Task.CompletedTask;
	private readonly TaskCompletionSource<bool> _flowFailure =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
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
	internal BattlePresenter Presenter => _presenter;

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
			() => _eventPresenter);
		_eventPresenter = new BattleEventPresenter(
			_boardGrid,
			_logLabel,
			() => _state,
			_skillPresentationController.Schedule);
		_boardController = new BattleBoardController(
			_boardGrid,
			_presenter,
			PlayerTeam,
			() => _state,
			() => _flowContext?.GetReachablePositions() ?? new Dictionary<GridPosition, int>());
		_actionPanelController = new BattleActionPanelController(
			_presenter,
			PlayerTeam,
			() => _state,
			() => _flowContext?.Engine,
			DispatchIntent,
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
			() => _flowContext?.IsAutoBattleEnabled == true,
			enabled => _flowContext?.SetAutoBattleEnabled(enabled),
			IsInsideTree,
			AppendLog);
		_settlementController = new BattleSettlementController(
			this,
			BattleSettlementPanelScene,
			_overlayRoot,
			_presenter,
			AppendLog);
		_boardGrid.CellActivated += position => DispatchIntent(new BattleUiIntent.ActivateCell(position));
		_boardGrid.HoveredCellChanged += position => DispatchIntent(new BattleUiIntent.HoverCell(position));
		_boardGrid.BackRequested += () => DispatchIntent(new BattleUiIntent.Back());
		_speedUpButton.Pressed += _settingsController.ToggleSpeedUp;
		_autoBattleButton.Pressed += () => DispatchIntent(new BattleUiIntent.ToggleAutoBattle());
		_actionPanelController.BindButtons();
		_settingsController.RefreshButtons();

		if (_isConfigured)
		{
			_startTask = StartBattleAsync();
		}
	}

	public async Task<bool> AwaitBattleAsync(CancellationToken cancellationToken = default)
	{
		var completionTask = _settlementController.AwaitAsync(cancellationToken);
		try
		{
			await _startTask.WaitAsync(cancellationToken);
			var completed = await Task.WhenAny(completionTask, _flowFailure.Task).WaitAsync(cancellationToken);
			await completed;
			return await completionTask;
		}
		catch
		{
			_settlementController.Cancel();
			if (GodotObject.IsInstanceValid(this)) QueueFree();
			throw;
		}
	}

	public override void _ExitTree()
	{
		_settingsController.RestoreTimeScale();
		base._ExitTree();
		_settlementController.Cancel();
	}

	private async Task StartBattleAsync()
	{
		if (_battleDefinition is null || _battleRequest is null)
		{
			throw new InvalidOperationException("Battle screen has not been configured.");
		}

		ApplyBattlePresentation(_battleDefinition);
		_state = GameRoot.BattleService.BuildBattleState(_battleRequest);
		_flowContext = new BattleFlowContext(this, _state);
		_flow = new BattleFlowStateMachine(_flowContext);
		_flow.BackgroundTaskFailed += exception => _flowFailure.TrySetException(exception);
		_settingsController.Load();
		_eventPresenter.Clear();
		AppendLog($"战斗开始：{_battleDefinition.Name}");
		await _flow.StartAsync();
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

	internal void RenderInteraction(BattleInteractionState interaction)
	{
		if (_state is null || !IsInsideTree())
		{
			return;
		}

		_interaction = interaction;
		var header = _presenter.CreateHeader(_state, interaction.Kind);
		_titleLabel.Text = _battleDefinition?.Name ?? header.Title;
		_subtitleLabel.Text = header.Subtitle;
		_boardController.RenderInteraction(interaction);
		_actionPanelController.RefreshActions(ResolveCapabilities(interaction));
		RefreshGlobalButtonAvailability(interaction);
		_settingsController.RefreshButtons();
	}

	internal void BeginActionPresentation(BattleInteractionState interaction) =>
		RenderInteraction(interaction);

	internal void CommitBattleStateToView(BattleInteractionState interaction)
	{
		if (_state is null || !IsInsideTree())
		{
			return;
		}

		_interaction = interaction;
		var header = _presenter.CreateHeader(_state, interaction.Kind);
		_titleLabel.Text = _battleDefinition?.Name ?? header.Title;
		_subtitleLabel.Text = header.Subtitle;
		_boardController.Commit(interaction);
		_actionPanelController.Render(interaction);
		_eventPresenter.Refresh();
		RefreshGlobalButtonAvailability(interaction);
		_settingsController.RefreshButtons();
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
			_startTask = StartBattleAsync();
		}
	}

	internal void AppendResult(BattleCommandResult<BattleActionResult> result) =>
		_eventPresenter.AppendResult(result);

	internal void AppendMessages(IReadOnlyList<BattleMessage> messages) =>
		_eventPresenter.AppendMessages(messages);

	internal void AppendLog(string text) => _eventPresenter.AppendLog(text);

	internal void BindState(BattleState state) => _state = state;

	internal Task ShowBattleEndedAsync(bool isWin)
	{
		_settingsController.Save();
		return _settlementController.CompleteAsync(isWin, _state, _battleRequest);
	}

	internal Task<InventoryEntry?> ShowItemPanelAsync() =>
		_actionPanelController.ShowItemPanelAsync();

	internal void ShowStatusPanel() => _actionPanelController.ShowStatusPanel();

	internal void PresentAutoBattleSetting(bool enabled) =>
		_settingsController.PresentAutoBattle(enabled);

	internal void RefreshGlobalControls()
	{
		if (_interaction is not null)
		{
			_actionPanelController.RefreshActions(ResolveCapabilities(_interaction));
			RefreshGlobalButtonAvailability(_interaction);
			_settingsController.RefreshButtons();
		}
	}

	internal void ApplyActingUnitFacing(BattleUnit actingUnit) => _boardGrid.ApplyUnitFacing(actingUnit.Id, actingUnit.Facing);

	internal Task PlayMoveAsync(BattleUnit actingUnit, IReadOnlyList<GridPosition> movementPath) =>
		_skillPresentationController.PlayMoveAsync(actingUnit, movementPath);

	internal Task PlayMoveRollbackAsync(BattleUnit actingUnit) =>
		_skillPresentationController.PlayMoveRollbackAsync(actingUnit);

	internal Task PlaySkillAsync(
		BattleUnit actingUnit,
		SkillInstance skill,
		BattleCommandResult<BattleActionResult> result) =>
		_skillPresentationController.PlaySkillAsync(actingUnit, skill, result);

	private BattleUiCapabilities ResolveCapabilities(BattleInteractionState interaction) =>
		_flowContext?.IsSurrenderRequested == true
			? interaction.Capabilities with
			{
				CanActivateBoard = false,
				CanSelectMove = false,
				CanSelectSkill = false,
				CanOpenItem = false,
				CanRest = false,
				CanEndAction = false,
				CanOpenStatus = false,
				CanSurrender = false,
			}
			: interaction.Capabilities;

	private void RefreshGlobalButtonAvailability(BattleInteractionState interaction)
	{
		_speedUpButton.Disabled = interaction.Kind == BattleFlowStateKind.BattleEnded;
		_autoBattleButton.Disabled = interaction.Kind == BattleFlowStateKind.BattleEnded ||
			_flowContext?.IsSurrenderRequested == true;
	}

	private void DispatchIntent(BattleUiIntent intent)
	{
		if (_flow is not null)
		{
			_ = ObserveIntentAsync(_flow.DispatchAsync(intent));
		}
	}

	private async Task ObserveIntentAsync(Task task)
	{
		try
		{
			await task;
		}
		catch (Exception exception)
		{
			_flowFailure.TrySetException(exception);
		}
	}

}
