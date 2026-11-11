using  Game.Core.Story;
namespace Game.Core.Battle;


public  record BattleEvent : StoryEvent;

public sealed record SegmentStartedEvent(
    string SegmentId) : BattleEvent;

public sealed record SegmentCompletedEvent(
    string SegmentId) : BattleEvent;

public sealed record DialogueReadyEvent(
    DialogueContext Dialogue) : BattleEvent;

public sealed record CommandExecutedEvent(
    string Name,
    IReadOnlyList<ExpressionValue> Args) : BattleEvent;

public sealed record VariableAssignedEvent(
    string Name,
    ExpressionValue Value) : BattleEvent;

public sealed record VariableDeletedEvent(
    string Name) : BattleEvent;

public sealed record ChoiceOfferedEvent(
    ChoiceContext Choice) : BattleEvent;

public sealed record ChoiceResolvedEvent(
    ChoiceContext Choice,
    int SelectedIndex) : BattleEvent;

public sealed record BattleStartedEvent(
    BattleContext Battle) : BattleEvent;

public sealed record BattleResolvedEvent(
    BattleContext Battle,
    BattleOutcome Outcome) : BattleEvent;

public sealed record JumpEvent(
    string Target) : BattleEvent;



public sealed record BattleContext(
    string BattleId,
    IReadOnlyList<BattleOutcome> AvailableOutcomes);

