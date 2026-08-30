using System.Text.Json.Serialization;
using Game.Core.Story;

namespace Game.Core.Persistence;

public sealed record StoryStateRecord(
    IReadOnlyDictionary<string, StoryVariableRecord> Variables,
    IReadOnlyList<string> CompletedStoryIds,
    string? LastStoryId,
    IReadOnlyList<StoryTimeKeyRecord>? TimeKeys = null,
    IReadOnlyList<StoryCompletionRecord>? CompletionProgress = null);

public sealed record StoryCompletionRecord(
    string StoryId,
    int Count,
    int LastCompletedTotalDays);

public sealed record StoryTimeKeyRecord(
    string Key,
    ClockRecord StartedAt,
    int LimitDays,
    ClockRecord DeadlineAt,
    string TargetStoryId);

public sealed record StoryVariableRecord(
    ExpressionValueKind Kind,
    bool Boolean,
    double Number,
    string? Text,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<StoryVariableRecord>? List = null)
{
    public static StoryVariableRecord FromExpressionValue(ExpressionValue value) =>
        new(
            value.Kind,
            value.Boolean,
            value.Number,
            value.Text,
            value.List?.Select(FromExpressionValue).ToList());

    public ExpressionValue ToExpressionValue() => Kind switch
    {
        ExpressionValueKind.Boolean => ExpressionValue.FromBoolean(Boolean),
        ExpressionValueKind.Number => ExpressionValue.FromNumber(Number),
        ExpressionValueKind.String => ExpressionValue.FromString(Text ?? string.Empty),
        ExpressionValueKind.List => ExpressionValue.FromList((List ?? []).Select(static item => item.ToExpressionValue()).ToList()),
        _ => throw new InvalidOperationException($"Unsupported story variable kind '{Kind}'."),
    };
}
