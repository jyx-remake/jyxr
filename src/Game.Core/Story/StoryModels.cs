namespace Game.Core.Story;

public sealed record StoryScript(
    int Version,
    IReadOnlyList<Segment> Segments);

public sealed record Segment(
    string Name,
    IReadOnlyList<Step> Steps);

public abstract record Step;

public sealed record DialogueStep(
    string Speaker,
    string Text) : Step;

public sealed record CommandStep(
    string Name,
    IReadOnlyList<ExprNode> Args) : Step;

public readonly record struct StoryCommandResult(string? JumpTarget)
{
    public static StoryCommandResult None { get; } = new(null);

    public static StoryCommandResult Jump(string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        return new StoryCommandResult(target);
    }
}

public sealed record JumpStep(
    string Target) : Step;

public sealed record ChoiceStep(
    ChoicePrompt Prompt,
    IReadOnlyList<ChoiceOption> Options) : Step;

public sealed record ChoicePrompt(
    string Speaker,
    string Text);

public sealed record ChoiceOption(
    string Text,
    IReadOnlyList<Step> Steps);

public sealed record BattleStep(
    string BattleId,
    IReadOnlyDictionary<BattleOutcome, IReadOnlyList<Step>> Outcomes) : Step;

public sealed record BranchStep(
    IReadOnlyList<BranchCase> Cases,
    IReadOnlyList<Step>? Fallback) : Step;

public sealed record BranchCase(
    ExprNode When,
    IReadOnlyList<Step> Steps);

public enum BattleOutcome
{
    Win,
    Lose,
    Timeout,
}
