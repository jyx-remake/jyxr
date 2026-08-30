using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Core.Model;

public sealed class StoryState
{
    private readonly Dictionary<string, ExpressionValue> _variables = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedStoryIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryCompletionRecord> _completionProgress = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryTimeKeyState> _timeKeys = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ExpressionValue> Variables => _variables;

    public IReadOnlyCollection<string> CompletedStoryIds => _completedStoryIds;

    public IReadOnlyDictionary<string, StoryCompletionRecord> CompletionProgress => _completionProgress;

    public IReadOnlyDictionary<string, StoryTimeKeyState> TimeKeys => _timeKeys;

    public string? LastStoryId { get; private set; }

    public static StoryState Restore(StoryStateRecord? record)
    {
        if (record is null)
        {
            return new StoryState();
        }

        var state = new StoryState();
        foreach (var (name, variable) in record.Variables)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ExpressionSymbol.Validate(name);
            state._variables.Add(name, variable.ToExpressionValue());
        }

        foreach (var storyId in record.CompletedStoryIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
            state._completedStoryIds.Add(storyId);
        }

        foreach (var progress in record.CompletionProgress ?? [])
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progress.StoryId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(progress.Count);
            ArgumentOutOfRangeException.ThrowIfNegative(progress.LastCompletedTotalDays);
            state._completedStoryIds.Add(progress.StoryId);
            state._completionProgress[progress.StoryId] = progress;
        }

        foreach (var storyId in state._completedStoryIds)
        {
            state._completionProgress.TryAdd(storyId, new StoryCompletionRecord(storyId, 1, 0));
        }

        foreach (var timeKeyRecord in record.TimeKeys ?? [])
        {
            var timeKey = StoryTimeKeyState.Restore(timeKeyRecord);
            state._timeKeys.Add(timeKey.Key, timeKey);
        }

        state.LastStoryId = string.IsNullOrWhiteSpace(record.LastStoryId)
            ? null
            : record.LastStoryId;
        return state;
    }

    public bool TryGetVariable(string name, out ExpressionValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ExpressionSymbol.Validate(name);
        return _variables.TryGetValue(name, out value);
    }

    public void SetVariable(string name, ExpressionValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ExpressionSymbol.Validate(name);
        if (_variables.TryGetValue(name, out var current) && current.Kind != value.Kind)
        {
            throw new InvalidOperationException(
                $"Story variable '{name}' is {current.Kind} and cannot be changed to {value.Kind}.");
        }

        _variables[name] = value;
    }

    public bool RemoveVariable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ExpressionSymbol.Validate(name);
        return _variables.Remove(name);
    }

    public StoryTimeKeyState SetTimeKey(
        string key,
        ClockState currentClock,
        int limitDays,
        string targetStoryId = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(currentClock);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitDays);

        var startedAt = currentClock.ToRecord();
        var deadlineClock = ClockState.Restore(startedAt);
        deadlineClock.AdvanceDays(limitDays);
        var timeKey = new StoryTimeKeyState(
            key,
            startedAt,
            limitDays,
            deadlineClock.ToRecord(),
            targetStoryId);
        _timeKeys[key] = timeKey;
        return timeKey;
    }

    public bool RemoveTimeKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _timeKeys.Remove(key);
    }

    public bool HasTimeKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _timeKeys.ContainsKey(key);
    }

    public bool IsStoryCompleted(string storyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        return _completedStoryIds.Contains(storyId);
    }

    public void MarkCompleted(string storyId, ClockState? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        _completedStoryIds.Add(storyId);
        var previous = _completionProgress.GetValueOrDefault(storyId);
        _completionProgress[storyId] = new StoryCompletionRecord(
            storyId,
            checked((previous?.Count ?? 0) + 1),
            clock?.TotalDays ?? previous?.LastCompletedTotalDays ?? 0);
    }

    public int GetCompletionCount(string storyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        return _completionProgress.GetValueOrDefault(storyId)?.Count ?? 0;
    }

    public int GetDaysSinceLastCompletion(string storyId, ClockState currentClock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        ArgumentNullException.ThrowIfNull(currentClock);
        return _completionProgress.TryGetValue(storyId, out var progress)
            ? Math.Max(0, currentClock.TotalDays - progress.LastCompletedTotalDays)
            : -1;
    }

    public void SetLastStory(string? storyId)
    {
        LastStoryId = string.IsNullOrWhiteSpace(storyId)
            ? null
            : storyId;
    }

    public StoryStateRecord ToRecord() =>
        new(
            _variables
                .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(
                    static entry => entry.Key,
                    static entry => StoryVariableRecord.FromExpressionValue(entry.Value),
                    StringComparer.Ordinal),
            _completedStoryIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            LastStoryId,
            _timeKeys.Values
                .OrderBy(static timeKey => timeKey.Key, StringComparer.Ordinal)
                .Select(static timeKey => timeKey.ToRecord())
                .ToArray(),
            _completionProgress.Values
                .OrderBy(static progress => progress.StoryId, StringComparer.Ordinal)
                .ToArray());
}

public sealed class StoryTimeKeyState
{
    public StoryTimeKeyState(
        string key,
        ClockRecord startedAt,
        int limitDays,
        ClockRecord deadlineAt,
        string targetStoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(startedAt);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitDays);
        ArgumentNullException.ThrowIfNull(deadlineAt);

        Key = key;
        StartedAt = startedAt;
        LimitDays = limitDays;
        DeadlineAt = deadlineAt;
        TargetStoryId = targetStoryId;
    }

    public string Key { get; }

    public ClockRecord StartedAt { get; }

    public int LimitDays { get; }

    public ClockRecord DeadlineAt { get; }

    public string TargetStoryId { get; }

    public static StoryTimeKeyState Restore(StoryTimeKeyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new StoryTimeKeyState(
            record.Key,
            record.StartedAt,
            record.LimitDays,
            record.DeadlineAt,
            record.TargetStoryId);
    }

    public StoryTimeKeyRecord ToRecord() =>
        new(Key, StartedAt, LimitDays, DeadlineAt, TargetStoryId);
}
