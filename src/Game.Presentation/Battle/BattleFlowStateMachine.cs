using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Presentation.Battle;

public enum BattleFlowStateKind
{
	WaitingTimeline,
	SelectingMove,
	UnitActing,
	SelectingSkillTarget,
	SelectingItem,
	SelectingItemTarget,
	PresentingAction,
	AutomatedTurn,
	BattleEnded,
}

public sealed record BattleUiCapabilities(
	bool CanActivateBoard = false,
	bool CanSelectMove = false,
	bool CanSelectSkill = false,
	bool CanOpenItem = false,
	bool CanRest = false,
	bool CanEndAction = false,
	bool CanOpenStatus = false,
	bool CanSurrender = true);

public sealed record BattleInteractionState(
	BattleFlowStateKind Kind,
	BattleUiCapabilities Capabilities,
	SkillInstance? SelectedSkill = null,
	InventoryEntry? SelectedItem = null,
	GridPosition? HoveredPosition = null);

public abstract record BattleUiIntent
{
	public sealed record HoverCell(GridPosition? Position) : BattleUiIntent;
	public sealed record ActivateCell(GridPosition Position) : BattleUiIntent;
	public sealed record SelectMove : BattleUiIntent;
	public sealed record Back : BattleUiIntent;
	public sealed record SelectSkill(SkillInstance Skill) : BattleUiIntent;
	public sealed record OpenItems : BattleUiIntent;
	public sealed record OpenStatus : BattleUiIntent;
	public sealed record Rest : BattleUiIntent;
	public sealed record EndAction : BattleUiIntent;
	public sealed record ToggleAutoBattle : BattleUiIntent;
	public sealed record Surrender : BattleUiIntent;
}

public interface IBattleFlowState
{
	BattleFlowStateKind Kind { get; }
	BattleInteractionState Interaction { get; }
	IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; }
	Task EnterAsync(BattleFlowStateMachine machine);
	Task ExitAsync(BattleFlowStateMachine machine);
	Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent);
	Task HandleGlobalAsync(BattleFlowStateMachine machine, BattleUiIntent intent);
}

public abstract class BattleFlowState : IBattleFlowState
{
	private static readonly IReadOnlySet<BattleFlowStateKind> NoTransitions = new HashSet<BattleFlowStateKind>();

	public abstract BattleFlowStateKind Kind { get; }
	public abstract BattleInteractionState Interaction { get; }
	public virtual IReadOnlySet<BattleFlowStateKind> AllowedTransitions => NoTransitions;
	public virtual Task EnterAsync(BattleFlowStateMachine machine) => Task.CompletedTask;
	public virtual Task ExitAsync(BattleFlowStateMachine machine) => Task.CompletedTask;
	public virtual Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent) => Task.CompletedTask;

	public virtual Task HandleGlobalAsync(BattleFlowStateMachine machine, BattleUiIntent intent)
	{
		switch (intent)
		{
			case BattleUiIntent.ToggleAutoBattle:
				machine.Context.ToggleAutoBattle();
				break;
			case BattleUiIntent.Surrender:
				machine.Context.RequestSurrender();
				break;
		}
		return Task.CompletedTask;
	}

	protected static IReadOnlySet<BattleFlowStateKind> Transitions(params BattleFlowStateKind[] kinds) =>
		new HashSet<BattleFlowStateKind>(kinds);
}

public sealed class BattleFlowStateMachine
{
	private readonly SemaphoreSlim _intentGate = new(1, 1);
	private IBattleFlowState? _current;
	private long _stateVersion;

	public BattleFlowStateMachine(IBattleFlowContext context)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public IBattleFlowContext Context { get; }
	public event Action<Exception>? BackgroundTaskFailed;
	public IBattleFlowState Current => _current ?? throw new InvalidOperationException("Battle flow has not started.");
	public bool IsPresentingAction => _current?.Kind == BattleFlowStateKind.PresentingAction;
	public bool IsBattleEnded => _current?.Kind == BattleFlowStateKind.BattleEnded;

	public async Task StartAsync(IBattleFlowState? initialState = null)
	{
		await _intentGate.WaitAsync();
		try
		{
			await SetInitialStateAsync(initialState ?? new WaitingTimelineState());
		}
		finally
		{
			_intentGate.Release();
		}
	}

	public Task DispatchAsync(BattleUiIntent intent)
	{
		ArgumentNullException.ThrowIfNull(intent);
		return intent is BattleUiIntent.ToggleAutoBattle or BattleUiIntent.Surrender
			? DispatchGlobalAsync(intent)
			: DispatchSerializedAsync(intent, _stateVersion);
	}

	public async Task TransitionAsync(IBattleFlowState next)
	{
		ArgumentNullException.ThrowIfNull(next);
		var current = Current;
		if (!current.AllowedTransitions.Contains(next.Kind))
		{
			throw new InvalidOperationException(
				$"Battle flow state '{current.Kind}' cannot transition to '{next.Kind}'.");
		}

		await current.ExitAsync(this);
		_current = next;
		_stateVersion++;
		await next.EnterAsync(this);
	}

	private async Task DispatchSerializedAsync(BattleUiIntent intent, long dispatchedStateVersion)
	{
		await _intentGate.WaitAsync();
		try
		{
			if (_current is not null && dispatchedStateVersion == _stateVersion)
			{
				await _current.HandleAsync(this, intent);
			}
		}
		finally
		{
			_intentGate.Release();
		}
	}

	private async Task DispatchGlobalAsync(BattleUiIntent intent)
	{
		var state = _current;
		if (state is null || state.Kind == BattleFlowStateKind.BattleEnded)
		{
			return;
		}

		await state.HandleGlobalAsync(this, intent);
		_ = ObserveResumeAfterGlobalIntentAsync();
	}

	private async Task ObserveResumeAfterGlobalIntentAsync()
	{
		try
		{
			await ResumeAfterGlobalIntentAsync();
		}
		catch (Exception exception)
		{
			BackgroundTaskFailed?.Invoke(exception);
		}
	}

	private async Task ResumeAfterGlobalIntentAsync()
	{
		await _intentGate.WaitAsync();
		try
		{
			if (_current is null || IsBattleEnded || IsPresentingAction)
			{
				return;
			}

			if (Context.IsSurrenderRequested)
			{
				await TransitionAsync(new BattleEndedState(isWin: false));
				return;
			}

			if (Context.IsAutoBattleEnabled &&
				_current.Kind is BattleFlowStateKind.SelectingMove or BattleFlowStateKind.UnitActing or
					BattleFlowStateKind.SelectingSkillTarget or BattleFlowStateKind.SelectingItemTarget)
			{
				var waitingState = new WaitingTimelineState();
				if (Context.ActingUnit is { } actingUnit)
				{
					await TransitionAsync(PresentingActionState.RollbackMove(
						actingUnit,
						waitingState,
						waitingState));
				}
				else
				{
					await TransitionAsync(waitingState);
				}
			}
		}
		finally
		{
			_intentGate.Release();
		}
	}

	private async Task SetInitialStateAsync(IBattleFlowState state)
	{
		_current = state;
		_stateVersion++;
		await state.EnterAsync(this);
	}
}
