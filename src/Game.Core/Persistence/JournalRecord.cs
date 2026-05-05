namespace Game.Core.Persistence;

public sealed record JournalRecord(
    IReadOnlyList<JournalEntryRecord> Entries);

public sealed record JournalEntryRecord(
    ClockRecord Timestamp,
    string Text);
