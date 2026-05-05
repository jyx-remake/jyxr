using Game.Core.Affix;

namespace Game.Core.Battle;

public sealed record BattleEvent(
    BattleEventKind Kind,
    string UnitId,
    HookTiming? Timing = null,
    IReadOnlyList<string>? HookLabels = null,
    string? Detail = null,
    BattleSpeechCue? Speech = null);

public sealed record BattleSpeechCue(string Text);

public enum BattleEventKind
{
    ActionStarted,
    Moved,
    MovementRolledBack,
    SkillCast,
    Damaged,
    ItemUsed,
    Rested,
    ActionEnded,
    BuffApplied,
    BuffExpired,
    HooksTriggered,
    Healed,
    MpDamaged,
    RageChanged,
    SpeechRequested,
}
