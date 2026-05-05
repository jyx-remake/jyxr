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
    string TargetStoryId,
    bool Triggered,
    ClockRecord? TriggeredAt);

public sealed record StoryVariableRecord(
    ExprValueKind Kind,
    bool Boolean,
    double Number,
    string? Text)
{
    public static StoryVariableRecord FromExprValue(ExprValue value) =>
        new(value.Kind, value.Boolean, value.Number, value.Text);

    public ExprValue ToExprValue() => Kind switch
    {
        ExprValueKind.Boolean => ExprValue.FromBoolean(Boolean),
        ExprValueKind.Number => ExprValue.FromNumber(Number),
        ExprValueKind.String => ExprValue.FromString(Text ?? string.Empty),
        _ => throw new InvalidOperationException($"Unsupported story variable kind '{Kind}'."),
    };
}
