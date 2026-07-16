using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Presentation.Battle;

namespace Game.Tests.Battle;

public sealed class BattleFlowStateMachineTests
{
	[Fact]
	public async Task TransitionRejectsStateNotAllowedByCurrentState()
	{
		var machine = new BattleFlowStateMachine(new FakeContext());
		await machine.StartAsync(new RecordingState(BattleFlowStateKind.SelectingMove));

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			machine.TransitionAsync(new RecordingState(BattleFlowStateKind.UnitActing)));
	}

	[Fact]
	public async Task QueuedIntentIsDroppedAfterOriginatingStateChanges()
	{
		var context = new FakeContext();
		var next = new RecordingState(BattleFlowStateKind.UnitActing);
		var origin = new BlockingTransitionState(next);
		var machine = new BattleFlowStateMachine(context);
		await machine.StartAsync(origin);

		var first = machine.DispatchAsync(new BattleUiIntent.Rest());
		await origin.HandlerEntered.Task;
		var stale = machine.DispatchAsync(new BattleUiIntent.EndAction());
		origin.ReleaseHandler.TrySetResult(true);
		await Task.WhenAll(first, stale);

		Assert.Same(next, machine.Current);
		Assert.Equal(0, next.HandledIntentCount);
	}

	[Fact]
	public async Task GlobalIntentBypassesLongRunningSerializedIntent()
	{
		var context = new FakeContext();
		var origin = new BlockingState();
		var machine = new BattleFlowStateMachine(context);
		await machine.StartAsync(origin);

		var blocked = machine.DispatchAsync(new BattleUiIntent.Rest());
		await origin.HandlerEntered.Task;
		await machine.DispatchAsync(new BattleUiIntent.ToggleAutoBattle());

		Assert.Equal(1, context.ToggleAutoCount);
		origin.ReleaseHandler.TrySetResult(true);
		await blocked;
	}

	[Fact]
	public async Task PresentingActionCommitsOnlyAfterPresentationCompletes()
	{
		var context = new FakeContext();
		var presentationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var releasePresentation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var committedState = new CommitState();
		var presenting = new PresentingActionState(
			new RecordingState(BattleFlowStateKind.SelectingMove),
			async _ =>
			{
				presentationStarted.TrySetResult(true);
				await releasePresentation.Task;
				return true;
			},
			_ => Task.FromResult<IBattleFlowState>(committedState));
		var machine = new BattleFlowStateMachine(context);

		var start = machine.StartAsync(presenting);
		await presentationStarted.Task;

		Assert.Equal(1, context.BeginPresentationCount);
		Assert.Equal(0, context.CommitCount);
		releasePresentation.TrySetResult(true);
		await start;

		Assert.Same(committedState, machine.Current);
		Assert.Equal(1, context.CommitCount);
	}

	[Fact]
	public void PresentingActionDisablesBoardAndActionCommandsButKeepsSurrender()
	{
		var state = new PresentingActionState(
			new RecordingState(BattleFlowStateKind.SelectingMove),
			_ => Task.FromResult(true),
			_ => Task.FromResult<IBattleFlowState>(new CommitState()));

		Assert.False(state.Interaction.Capabilities.CanActivateBoard);
		Assert.False(state.Interaction.Capabilities.CanSelectSkill);
		Assert.False(state.Interaction.Capabilities.CanRest);
		Assert.True(state.Interaction.Capabilities.CanSurrender);
	}

	[Fact]
	public async Task CancellingItemPanelRestoresTheOriginatingState()
	{
		var context = new FakeContext { ItemSelection = Task.FromResult<InventoryEntry?>(null) };
		var origin = new RecordingState(BattleFlowStateKind.SelectingMove);
		var machine = new BattleFlowStateMachine(context);

		await machine.StartAsync(new SelectingItemState(origin));

		Assert.Same(origin, machine.Current);
	}

	[Fact]
	public async Task SurrenderDuringPresentationWaitsForPresentationBeforeEndingBattle()
	{
		var context = new FakeContext();
		var presentationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var releasePresentation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var presenting = new PresentingActionState(
			new RecordingState(BattleFlowStateKind.SelectingMove),
			async _ =>
			{
				presentationStarted.TrySetResult(true);
				await releasePresentation.Task;
				return true;
			},
			_ => Task.FromResult<IBattleFlowState>(new CommitState()));
		var machine = new BattleFlowStateMachine(context);

		var start = machine.StartAsync(presenting);
		await presentationStarted.Task;
		await machine.DispatchAsync(new BattleUiIntent.Surrender());

		Assert.Equal(0, context.CompleteBattleCount);
		releasePresentation.TrySetResult(true);
		await start;

		Assert.Equal(BattleFlowStateKind.BattleEnded, machine.Current.Kind);
		Assert.Equal(1, context.CompleteBattleCount);
	}

	private class RecordingState(BattleFlowStateKind kind) : BattleFlowState
	{
		public int HandledIntentCount { get; private set; }
		public override BattleFlowStateKind Kind => kind;
		public override BattleInteractionState Interaction { get; } = new(kind, new BattleUiCapabilities());

		public override Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent)
		{
			HandledIntentCount++;
			return Task.CompletedTask;
		}
	}

	private sealed class CommitState : RecordingState
	{
		public CommitState() : base(BattleFlowStateKind.UnitActing)
		{
		}

		public override Task EnterAsync(BattleFlowStateMachine machine)
		{
			machine.Context.CommitBattleStateToView(Interaction);
			return Task.CompletedTask;
		}
	}

	private sealed class BlockingTransitionState(IBattleFlowState next) : RecordingState(BattleFlowStateKind.SelectingMove)
	{
		public TaskCompletionSource<bool> HandlerEntered { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<bool> ReleaseHandler { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } =
			new HashSet<BattleFlowStateKind> { BattleFlowStateKind.UnitActing };

		public override async Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent)
		{
			HandlerEntered.TrySetResult(true);
			await ReleaseHandler.Task;
			await machine.TransitionAsync(next);
		}
	}

	private sealed class BlockingState : RecordingState
	{
		public BlockingState() : base(BattleFlowStateKind.SelectingMove)
		{
		}

		public TaskCompletionSource<bool> HandlerEntered { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<bool> ReleaseHandler { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override async Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent)
		{
			HandlerEntered.TrySetResult(true);
			await ReleaseHandler.Task;
		}
	}

	private sealed class FakeContext : IBattleFlowContext
	{
		public int ToggleAutoCount { get; private set; }
		public int BeginPresentationCount { get; private set; }
		public int CommitCount { get; private set; }
		public int CompleteBattleCount { get; private set; }
		public Task<InventoryEntry?> ItemSelection { get; init; } =
			Task.FromResult<InventoryEntry?>(null);
		public BattleState State => throw new NotSupportedException();
		public BattleEngine Engine => throw new NotSupportedException();
		public bool IsAutoBattleEnabled { get; private set; }
		public bool IsSurrenderRequested { get; private set; }
		public int PlayerTeam => 0;
		public BattleUnit? ActingUnit => null;
		public void RenderInteraction(BattleInteractionState interaction) { }
		public void CommitBattleStateToView(BattleInteractionState interaction) => CommitCount++;
		public void BeginActionPresentation(BattleInteractionState interaction) => BeginPresentationCount++;
		public void ToggleAutoBattle() { ToggleAutoCount++; IsAutoBattleEnabled = !IsAutoBattleEnabled; }
		public void RequestSurrender() => IsSurrenderRequested = true;
		public BattleUnit AdvanceTimelineToNextAction() => throw new NotSupportedException();
		public BattleTurnPlan DecideAutomatedTurn(BattleUnit actingUnit) => throw new NotSupportedException();
		public void AppendTurnLog(BattleUnit unit, bool isAutomated) => throw new NotSupportedException();
		public Task<bool> ExecuteMoveAsync(BattleUnit actingUnit, GridPosition destination) => throw new NotSupportedException();
		public Task<bool> ExecuteSkillAsync(BattleUnit actingUnit, SkillInstance skill, GridPosition target) => throw new NotSupportedException();
		public bool ExecuteItem(BattleUnit actingUnit, InventoryEntry item, string targetUnitId) => throw new NotSupportedException();
		public bool ExecuteRest(BattleUnit actingUnit) => throw new NotSupportedException();
		public bool ExecuteEndAction(BattleUnit actingUnit) => throw new NotSupportedException();
		public Task<bool> RollbackMoveAsync(BattleUnit actingUnit) => throw new NotSupportedException();
		public Task RollbackPlayerMoveForAutoBattleAsync() => Task.CompletedTask;
		public SkillInstance? ResolveDefaultSkill(BattleUnit actingUnit) => throw new NotSupportedException();
		public SkillInstance? ResolveSkill(BattleUnit actingUnit, string? skillId) => throw new NotSupportedException();
		public Task<InventoryEntry?> ShowItemPanelAsync() => ItemSelection;
		public void ShowStatusPanel() => throw new NotSupportedException();
		public bool TryGetBattleResult(out bool isWin) { isWin = false; return IsSurrenderRequested; }
		public Task CompleteBattleAsync(bool isWin) { CompleteBattleCount++; return Task.CompletedTask; }
	}
}
