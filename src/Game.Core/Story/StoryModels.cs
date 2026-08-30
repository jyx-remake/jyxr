namespace Game.Core.Story;

public sealed record StoryScript(
    int Version,
    IReadOnlyList<Segment> Segments)
{
    public const int CurrentVersion = 3;
}

public sealed record Segment(
    string Name,
    IReadOnlyList<Step> Steps);

public abstract record Step;

public sealed record DialogueStep(
    string Speaker,
    string Text) : Step;

public sealed record CommandStep(
    ParsedCall Call) : Step;

public sealed record SetVariableStep(
    string Target,
    ParsedExpression Value) : Step;

public sealed record DeleteVariableStep(
    string Target) : Step;

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

public sealed record CallStep(
    string Target) : Step;

public sealed record ReturnStep : Step;

public sealed record ChoiceStep(
    ChoicePrompt Prompt,
    IReadOnlyList<ChoiceBlock> Blocks,
    ChoiceStyle Style = ChoiceStyle.Regular) : Step;

public enum ChoiceStyle
{
    Regular,
    Bold,
}

public sealed record ChoicePrompt(
    string Speaker,
    string Text);

public abstract record ChoiceBlock;

public sealed record ChoiceOptionsBlock(
    IReadOnlyList<ChoiceOption> Options) : ChoiceBlock;

public sealed record ChoiceBranchBlock(
    IReadOnlyList<ChoiceBranchCase> Cases,
    IReadOnlyList<ChoiceOption>? Fallback) : ChoiceBlock;

public sealed record ChoiceBranchCase(
    ParsedExpression When,
    IReadOnlyList<ChoiceOption> Options);

public sealed record ChoiceOption(
    string Text,
    ParsedExpression? When,
    IReadOnlyList<Step> Steps);

public sealed record BattleStep(
    string BattleId,
    IReadOnlyDictionary<BattleOutcome, IReadOnlyList<Step>> Outcomes,
    int TotalBattles = 1,
    int BattleLevel = 0) : Step;

public sealed record BranchStep(
    IReadOnlyList<BranchCase> Cases,
    IReadOnlyList<Step>? Fallback) : Step;

public sealed record BranchCase(
    ParsedExpression When,
    IReadOnlyList<Step> Steps);

public enum BattleOutcome
{
    Win,
    Lose,
    Timeout,
}
