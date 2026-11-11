

namespace Game.Core.Battle;

public sealed record BattleEventSegmentEntry(
    string Id,
    string ScriptId,
    BattleEventScript Script,
    BattleMainSegment Segment);
