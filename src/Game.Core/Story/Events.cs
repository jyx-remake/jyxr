namespace Game.Core.Story;

public abstract record StoryEvent;

public sealed record SegmentStartedEvent(
    string SegmentId) : StoryEvent;

public sealed record SegmentCompletedEvent(
    string SegmentId) : StoryEvent;

public sealed record DialogueReadyEvent(
    DialogueContext Dialogue) : StoryEvent;

public sealed record CommandExecutedEvent(
    string Name,
    IReadOnlyList<ExprValue> Args) : StoryEvent;

public sealed record ChoiceOfferedEvent(
    ChoiceContext Choice) : StoryEvent;

public sealed record ChoiceResolvedEvent(
    ChoiceContext Choice,
    int SelectedIndex) : StoryEvent;

public sealed record BattleStartedEvent(
    BattleContext Battle) : StoryEvent;

public sealed record BattleResolvedEvent(
    BattleContext Battle,
    BattleOutcome Outcome) : StoryEvent;

public sealed record JumpEvent(
    string Target) : StoryEvent;

public sealed record DialogueContext(
    string Speaker,
    string Text);

public sealed record ChoiceContext(
    string PromptSpeaker,
    string PromptText,
    IReadOnlyList<ChoiceOptionView> Options);

public sealed record ChoiceOptionView(
    int Index,
    string Text);

public sealed record BattleContext(
    string BattleId,
    IReadOnlyList<BattleOutcome> AvailableOutcomes);
