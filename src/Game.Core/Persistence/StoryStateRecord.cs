using System.Text.Json.Serialization;
using Game.Core.Story;

namespace Game.Core.Persistence;

public sealed record StoryStateRecord(
    IReadOnlyDictionary<string, StoryVariableRecord> Variables,
    IReadOnlyList<string> CompletedStoryIds,
    string? LastStoryId,
    IReadOnlyList<StoryTimeKeyRecord>? TimeKeys = null);

public sealed record StoryTimeKeyRecord(
    string Key,
    ClockRecord StartedAt,
    int LimitDays,
    ClockRecord DeadlineAt,
    string TargetStoryId);

public sealed record StoryVariableRecord(
    ExprValueKind Kind,
    bool Boolean,
    double Number,
    string? Text,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<StoryVariableRecord>? List = null)
{
    public static StoryVariableRecord FromExprValue(ExprValue value) =>
        new(
            value.Kind,
            value.Boolean,
            value.Number,
            value.Text,
            value.List?.Select(FromExprValue).ToList());

    public ExprValue ToExprValue() => Kind switch
    {
        ExprValueKind.Boolean => ExprValue.FromBoolean(Boolean),
        ExprValueKind.Number => ExprValue.FromNumber(Number),
        ExprValueKind.String => ExprValue.FromString(Text ?? string.Empty),
        ExprValueKind.List => ExprValue.FromList((List ?? []).Select(static item => item.ToExprValue()).ToList()),
        _ => throw new InvalidOperationException($"Unsupported story variable kind '{Kind}'."),
    };
}
