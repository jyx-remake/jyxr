namespace Game.Core.Story;

public sealed record StorySegmentEntry(
    string Id,
    string ScriptId,
    StoryScript Script,
    Segment Segment);
