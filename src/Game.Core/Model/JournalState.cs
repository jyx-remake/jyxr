using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class JournalState
{
    private readonly List<JournalEntry> _entries = [];

    public IReadOnlyList<JournalEntry> Entries => _entries;

    public static JournalState Restore(JournalRecord? record)
    {
        var state = new JournalState();
        if (record is null)
        {
            return state;
        }

        foreach (var entry in record.Entries)
        {
            state._entries.Add(JournalEntry.Restore(entry));
        }

        return state;
    }

    public void Append(ClockState clock, string text)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        _entries.Add(JournalEntry.FromClock(clock, text));
    }

    public JournalRecord ToRecord() =>
        new(_entries.Select(static entry => entry.ToRecord()).ToArray());
}

public sealed record JournalEntry(
    ClockRecord Timestamp,
    string Text)
{
    public static JournalEntry FromClock(ClockState clock, string text)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new JournalEntry(new ClockRecord(clock.Year, clock.Month, clock.Day, clock.TimeSlot), text);
    }

    public static JournalEntry Restore(JournalEntryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Timestamp);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.Text);

        return new JournalEntry(record.Timestamp, record.Text);
    }

    public JournalEntryRecord ToRecord() => new(Timestamp, Text);
}
