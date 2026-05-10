using Game.Core.Definitions;
using Game.Core;
using Game.Core.Model;

namespace Game.Application;

public sealed class WorldTriggerService
{
    private readonly GameSession _session;
    private readonly MapConditionEvaluator _conditionEvaluator;

    public WorldTriggerService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _conditionEvaluator = new MapConditionEvaluator(session);
    }

    private GameState State => _session.State;

    public MapInteractionResult? ResolvePendingTrigger()
    {
        if (State.WorldTriggers.IsBlocked)
        {
            return null;
        }

        foreach (var trigger in _session.ContentRepository.GetWorldTriggers())
        {
            if (IsCompleted(trigger))
            {
                continue;
            }

            if (!_conditionEvaluator.AreSatisfied(trigger.Conditions))
            {
                continue;
            }

            if (!Probability.RollPercentage(trigger.Probability))
            {
                continue;
            }

            MarkCompletedIfNeeded(trigger);
            return BuildInteractionResult(trigger);
        }

        return null;
    }

    private bool IsCompleted(WorldTriggerDefinition trigger)
    {
        if (trigger.RepeatMode != RepeatMode.Once)
        {
            return false;
        }

        if (State.WorldTriggers.IsCompleted(trigger.Id))
        {
            return true;
        }

        return string.Equals(trigger.Type, "story", StringComparison.Ordinal) &&
            State.Story.IsStoryCompleted(trigger.TargetId);
    }

    private void MarkCompletedIfNeeded(WorldTriggerDefinition trigger)
    {
        if (trigger.RepeatMode == RepeatMode.Once)
        {
            State.WorldTriggers.MarkCompleted(trigger.Id);
        }
    }

    public void Block() => State.WorldTriggers.Block();

    public void Unblock() => State.WorldTriggers.Unblock();

    private static MapInteractionResult BuildInteractionResult(WorldTriggerDefinition trigger) =>
        trigger.Type switch
        {
            "story" => BuildInteractionResult(MapService.MapInteractionOutcome.StoryRequested, trigger),
            "shop" => BuildInteractionResult(MapService.MapInteractionOutcome.ShopRequested, trigger),
            "xiangzi" => BuildInteractionResult(MapService.MapInteractionOutcome.ChestRequested, trigger),
            "battle" => BuildInteractionResult(MapService.MapInteractionOutcome.BattleRequested, trigger),
            _ => BuildInteractionResult(MapService.MapInteractionOutcome.PlaceholderInteraction, trigger),
        };

    private static MapInteractionResult BuildInteractionResult(
        MapService.MapInteractionOutcome outcome,
        WorldTriggerDefinition trigger) =>
        new()
        {
            Outcome = outcome,
            Message = trigger.Description,
            TargetId = trigger.TargetId,
            ConsumedTimeSlots = 0,
        };

}
