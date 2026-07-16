using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Presentation.Battle;

public sealed class WaitingTimelineState : BattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.WaitingTimeline,
		new BattleUiCapabilities());

	public override BattleFlowStateKind Kind => BattleFlowStateKind.WaitingTimeline;
	public override BattleInteractionState Interaction => ViewState;
	public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } = Transitions(
		BattleFlowStateKind.SelectingMove,
		BattleFlowStateKind.AutomatedTurn,
		BattleFlowStateKind.BattleEnded);

	public override async Task EnterAsync(BattleFlowStateMachine machine)
	{
		var context = machine.Context;
		context.RenderInteraction(Interaction);
		if (context.TryGetBattleResult(out var isWin))
		{
			await machine.TransitionAsync(new BattleEndedState(isWin));
			return;
		}

		var actingUnit = context.ActingUnit ?? context.AdvanceTimelineToNextAction();
		if (actingUnit.Team == context.PlayerTeam && !context.IsAutoBattleEnabled)
		{
			context.AppendTurnLog(actingUnit, isAutomated: false);
			await machine.TransitionAsync(new SelectingMoveState());
			return;
		}

		context.AppendTurnLog(actingUnit, isAutomated: true);
		await machine.TransitionAsync(new AutomatedTurnState());
	}
}

public abstract class InteractiveBattleFlowState : BattleFlowState
{
	protected static readonly BattleUiCapabilities InteractiveCapabilities = new(
		CanActivateBoard: true,
		CanSelectMove: true,
		CanSelectSkill: true,
		CanOpenItem: true,
		CanRest: true,
		CanEndAction: true,
		CanOpenStatus: true);

	public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } = Transitions(
		BattleFlowStateKind.WaitingTimeline,
		BattleFlowStateKind.SelectingMove,
		BattleFlowStateKind.UnitActing,
		BattleFlowStateKind.SelectingSkillTarget,
		BattleFlowStateKind.SelectingItem,
		BattleFlowStateKind.SelectingItemTarget,
		BattleFlowStateKind.PresentingAction,
		BattleFlowStateKind.BattleEnded);

	public override Task EnterAsync(BattleFlowStateMachine machine)
	{
		machine.Context.CommitBattleStateToView(Interaction);
		return Task.CompletedTask;
	}

	public override async Task HandleAsync(BattleFlowStateMachine machine, BattleUiIntent intent)
	{
		var context = machine.Context;
		var actingUnit = context.ActingUnit;
		if (actingUnit is null || actingUnit.Team != context.PlayerTeam || context.IsSurrenderRequested)
		{
			return;
		}

		switch (intent)
		{
			case BattleUiIntent.SelectMove:
				await machine.TransitionAsync(PresentingActionState.RollbackMove(
					actingUnit,
					new SelectingMoveState(),
					new SelectingMoveState()));
				break;
			case BattleUiIntent.Back:
				await HandleBackAsync(machine, actingUnit);
				break;
			case BattleUiIntent.SelectSkill(var skill):
				await machine.TransitionAsync(new SelectingSkillTargetState(skill));
				break;
			case BattleUiIntent.OpenItems:
				await machine.TransitionAsync(new SelectingItemState(this));
				break;
			case BattleUiIntent.OpenStatus:
				context.ShowStatusPanel();
				break;
			case BattleUiIntent.Rest:
				if (context.ExecuteRest(actingUnit))
				{
					await machine.TransitionAsync(new WaitingTimelineState());
				}
				else
				{
					context.CommitBattleStateToView(Interaction);
				}
				break;
			case BattleUiIntent.EndAction:
				if (context.ExecuteEndAction(actingUnit))
				{
					await machine.TransitionAsync(new WaitingTimelineState());
				}
				else
				{
					context.CommitBattleStateToView(Interaction);
				}
				break;
			default:
				await HandleInteractiveIntentAsync(machine, actingUnit, intent);
				break;
		}
	}

	protected virtual Task HandleInteractiveIntentAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit,
		BattleUiIntent intent) => Task.CompletedTask;

	protected virtual async Task HandleBackAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit)
	{
		await machine.TransitionAsync(PresentingActionState.RollbackMove(
			actingUnit,
			this,
			new SelectingMoveState()));
	}

	protected static async Task<IBattleFlowState> ResolvePostMoveStateAsync(
		IBattleFlowContext context,
		BattleUnit actingUnit)
	{
		if (context.IsAutoBattleEnabled)
		{
			await context.RollbackMoveAsync(actingUnit);
			return new WaitingTimelineState();
		}

		var defaultSkill = context.ResolveDefaultSkill(actingUnit);
		return defaultSkill is null
			? new UnitActingState()
			: new SelectingSkillTargetState(defaultSkill);
	}
}

public sealed class SelectingMoveState : InteractiveBattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.SelectingMove,
		InteractiveCapabilities);

	public override BattleFlowStateKind Kind => BattleFlowStateKind.SelectingMove;
	public override BattleInteractionState Interaction => ViewState;

	protected override async Task HandleInteractiveIntentAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit,
		BattleUiIntent intent)
	{
		if (intent is not BattleUiIntent.ActivateCell(var position))
		{
			return;
		}

		await machine.TransitionAsync(new PresentingActionState(
			this,
			context => context.ExecuteMoveAsync(actingUnit, position),
			context => ResolvePostMoveStateAsync(context, actingUnit)));
	}
}

public sealed class UnitActingState : InteractiveBattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.UnitActing,
		InteractiveCapabilities with { CanActivateBoard = false });

	public override BattleFlowStateKind Kind => BattleFlowStateKind.UnitActing;
	public override BattleInteractionState Interaction => ViewState;
}

public sealed class SelectingSkillTargetState : InteractiveBattleFlowState
{
	private GridPosition? _hoveredPosition;

	public SelectingSkillTargetState(SkillInstance skill)
	{
		Skill = skill ?? throw new ArgumentNullException(nameof(skill));
	}

	public SkillInstance Skill { get; }
	public override BattleFlowStateKind Kind => BattleFlowStateKind.SelectingSkillTarget;
	public override BattleInteractionState Interaction => new(
		Kind,
		InteractiveCapabilities,
		SelectedSkill: Skill,
		HoveredPosition: _hoveredPosition);

	protected override async Task HandleInteractiveIntentAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit,
		BattleUiIntent intent)
	{
		switch (intent)
		{
			case BattleUiIntent.HoverCell(var position):
				if (_hoveredPosition == position)
				{
					return;
				}
				_hoveredPosition = position;
				machine.Context.RenderInteraction(Interaction);
				break;
			case BattleUiIntent.ActivateCell(var position):
				await machine.TransitionAsync(new PresentingActionState(
					this,
					context => context.ExecuteSkillAsync(actingUnit, Skill, position),
					_ => Task.FromResult<IBattleFlowState>(new WaitingTimelineState())));
				break;
		}
	}
}

public sealed class SelectingItemState(IBattleFlowState returnState) : BattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.SelectingItem,
		new BattleUiCapabilities(CanSurrender: true));

	public override BattleFlowStateKind Kind => BattleFlowStateKind.SelectingItem;
	public override BattleInteractionState Interaction => ViewState;
	public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } = Transitions(
		BattleFlowStateKind.SelectingMove,
		BattleFlowStateKind.UnitActing,
		BattleFlowStateKind.SelectingSkillTarget,
		BattleFlowStateKind.SelectingItemTarget,
		BattleFlowStateKind.BattleEnded);

	public override async Task EnterAsync(BattleFlowStateMachine machine)
	{
		machine.Context.CommitBattleStateToView(Interaction);
		var item = await machine.Context.ShowItemPanelAsync();
		if (machine.Context.IsSurrenderRequested)
		{
			await machine.TransitionAsync(new BattleEndedState(isWin: false));
			return;
		}

		await machine.TransitionAsync(item is null
			? returnState
			: new SelectingItemTargetState(item, returnState));
	}
}

public sealed class SelectingItemTargetState(
	InventoryEntry item,
	IBattleFlowState returnState) : InteractiveBattleFlowState
{
	public InventoryEntry Item { get; } = item ?? throw new ArgumentNullException(nameof(item));
	public override BattleFlowStateKind Kind => BattleFlowStateKind.SelectingItemTarget;
	public override BattleInteractionState Interaction => new(
		Kind,
		InteractiveCapabilities,
		SelectedItem: Item);

	protected override async Task HandleInteractiveIntentAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit,
		BattleUiIntent intent)
	{
		if (intent is not BattleUiIntent.ActivateCell(var position) ||
			machine.Context.State.GetUnitAt(position) is not { } target)
		{
			return;
		}

		if (machine.Context.ExecuteItem(actingUnit, Item, target.Id))
		{
			await machine.TransitionAsync(new WaitingTimelineState());
		}
		else
		{
			machine.Context.CommitBattleStateToView(Interaction);
		}
	}

	protected override Task HandleBackAsync(
		BattleFlowStateMachine machine,
		BattleUnit actingUnit) => machine.TransitionAsync(returnState);
}

public sealed class PresentingActionState(
	IBattleFlowState failureState,
	Func<IBattleFlowContext, Task<bool>> execute,
	Func<IBattleFlowContext, Task<IBattleFlowState>> resolveSuccessState) : BattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.PresentingAction,
		new BattleUiCapabilities());

	public override BattleFlowStateKind Kind => BattleFlowStateKind.PresentingAction;
	public override BattleInteractionState Interaction => ViewState;
	public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } = Transitions(
		BattleFlowStateKind.WaitingTimeline,
		BattleFlowStateKind.SelectingMove,
		BattleFlowStateKind.UnitActing,
		BattleFlowStateKind.SelectingSkillTarget,
		BattleFlowStateKind.AutomatedTurn,
		BattleFlowStateKind.BattleEnded);

	public static PresentingActionState RollbackMove(
		BattleUnit actingUnit,
		IBattleFlowState failureState,
		IBattleFlowState successState) => new(
			failureState,
			context => context.RollbackMoveAsync(actingUnit),
			_ => Task.FromResult(successState));

	public override async Task EnterAsync(BattleFlowStateMachine machine)
	{
		var context = machine.Context;
		context.BeginActionPresentation(Interaction);
		if (!await execute(context))
		{
			await machine.TransitionAsync(failureState);
			return;
		}

		if (context.TryGetBattleResult(out var isWin))
		{
			await machine.TransitionAsync(new BattleEndedState(isWin));
			return;
		}

		await machine.TransitionAsync(await resolveSuccessState(context));
	}
}

public sealed class AutomatedTurnState(
	BattleTurnPlan? plan = null,
	bool movementCompleted = false,
	bool mainActionAttempted = false)
	: BattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.AutomatedTurn,
		new BattleUiCapabilities());

	public override BattleFlowStateKind Kind => BattleFlowStateKind.AutomatedTurn;
	public override BattleInteractionState Interaction => ViewState;
	public override IReadOnlySet<BattleFlowStateKind> AllowedTransitions { get; } = Transitions(
		BattleFlowStateKind.PresentingAction,
		BattleFlowStateKind.WaitingTimeline,
		BattleFlowStateKind.BattleEnded);

	public override async Task EnterAsync(BattleFlowStateMachine machine)
	{
		var context = machine.Context;
		context.CommitBattleStateToView(Interaction);
		var actingUnit = context.ActingUnit ??
			throw new InvalidOperationException("Automated turn has no acting unit.");
		var turnPlan = plan ?? context.DecideAutomatedTurn(actingUnit);

		if (!movementCompleted && turnPlan.MoveDestination != actingUnit.Position)
		{
			await machine.TransitionAsync(new PresentingActionState(
				new AutomatedTurnState(turnPlan, movementCompleted: true, mainActionAttempted),
				flow => flow.ExecuteMoveAsync(actingUnit, turnPlan.MoveDestination),
				flow => Task.FromResult(ResolveAfterAutomatedMove(flow, actingUnit, turnPlan))));
			return;
		}

		if (!mainActionAttempted &&
			turnPlan.MainAction.Kind == BattleMainActionKind.CastSkill &&
			turnPlan.MainAction.TargetPosition is { } targetPosition &&
			context.ResolveSkill(actingUnit, turnPlan.MainAction.SkillId) is { } skill)
		{
			await machine.TransitionAsync(new PresentingActionState(
				new AutomatedTurnState(turnPlan, movementCompleted: true, mainActionAttempted: true),
				flow => flow.ExecuteSkillAsync(actingUnit, skill, targetPosition),
				_ => Task.FromResult<IBattleFlowState>(new WaitingTimelineState())));
			return;
		}

		context.ExecuteRest(actingUnit);
		await machine.TransitionAsync(new WaitingTimelineState());
	}

	private static IBattleFlowState ResolveAfterAutomatedMove(
		IBattleFlowContext context,
		BattleUnit actingUnit,
		BattleTurnPlan turnPlan)
	{
		if (actingUnit.Team == context.PlayerTeam && !context.IsAutoBattleEnabled)
		{
			var defaultSkill = context.ResolveDefaultSkill(actingUnit);
			return defaultSkill is null
				? new UnitActingState()
				: new SelectingSkillTargetState(defaultSkill);
		}

		return new AutomatedTurnState(turnPlan, movementCompleted: true);
	}
}

public sealed class BattleEndedState(bool isWin) : BattleFlowState
{
	private static readonly BattleInteractionState ViewState = new(
		BattleFlowStateKind.BattleEnded,
		new BattleUiCapabilities(CanSurrender: false));

	public override BattleFlowStateKind Kind => BattleFlowStateKind.BattleEnded;
	public override BattleInteractionState Interaction => ViewState;

	public override async Task EnterAsync(BattleFlowStateMachine machine)
	{
		machine.Context.CommitBattleStateToView(Interaction);
		await machine.Context.CompleteBattleAsync(isWin);
	}
}
