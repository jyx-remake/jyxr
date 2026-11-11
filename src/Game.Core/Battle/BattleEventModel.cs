
using Game.Core.Story;

namespace Game.Core.Battle;

public sealed record BattleEventScript
(
    int Version,
    IReadOnlyList<BattleMainSegment> Segments)
{
    public const int CurrentVersion = 3;
}

    public sealed record BattleMainSegment(
    string Name,
    List<BattleSmallSegment> BattleSmallSegments);


    public sealed record BattleSmallSegment(
    IReadOnlyList<BranchCase> Cases,
    IReadOnlyList<BattleStep>? Fallback,
    int? MaxExecutions = null);

    public sealed record BranchCase(
    ParsedExpression? When,
    IReadOnlyList<BattleStep> Steps);
   
   public abstract record BattleStep : Step;
   public sealed record ReturnStep : BattleStep;
   public sealed record JumpStep(
    string Target) : BattleStep;

    public sealed record CallStep(
    string Target) : BattleStep;
   public sealed record SetVariableStep(
    string Target,
    ParsedExpression Value) : BattleStep;
   public sealed record DeleteVariableStep(
    string Target) : BattleStep;
    public sealed record BattleCommandStep(
        ParsedCall Call) : BattleStep;
    //读取story
    public sealed record BattleInvokeStory(
        String StoryId) : BattleStep;
    public sealed record DialogueStep(
    string Speaker,
    string Text) : BattleStep;
    public sealed record CommandStep(
    ParsedCall Call) : BattleStep;
    public sealed record ChoiceStep(
    ChoicePrompt Prompt,
    IReadOnlyList<ChoiceBlock> Blocks,
    ChoiceStyle Style = ChoiceStyle.Regular) : BattleStep;
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
        IReadOnlyList<BattleStep> Steps);




