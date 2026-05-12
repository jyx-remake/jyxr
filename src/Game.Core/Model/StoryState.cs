using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Core.Model;

public sealed class StoryState
{
    private readonly Dictionary<string, ExprValue> _variables = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedStoryIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryTimeKeyState> _timeKeys = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ExprValue> Variables => _variables;

    public IReadOnlyCollection<string> CompletedStoryIds => _completedStoryIds;

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
            state._variables.Add(name, variable.ToExprValue());
        }

        foreach (var storyId in record.CompletedStoryIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
            state._completedStoryIds.Add(storyId);
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

    public bool TryGetVariable(string name, out ExprValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _variables.TryGetValue(name, out value);
    }

    public void SetVariable(string name, ExprValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _variables[name] = value;
    }

    public bool RemoveVariable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
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

    public void MarkCompleted(string storyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        _completedStoryIds.Add(storyId);
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
                    static entry => StoryVariableRecord.FromExprValue(entry.Value),
                    StringComparer.Ordinal),
            _completedStoryIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            LastStoryId,
            _timeKeys.Values
                .OrderBy(static timeKey => timeKey.Key, StringComparer.Ordinal)
                .Select(static timeKey => timeKey.ToRecord())
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
