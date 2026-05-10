using Game.Core.Affix;

namespace Game.Core.Battle;

public sealed record BattleEvent(
    BattleEventKind Kind,
    string UnitId,
    HookTiming? Timing = null,
    IReadOnlyList<string>? HookLabels = null,
    string? Detail = null,
    BattleSpeechCue? Speech = null,
    BattleSkillCastInfo? SkillCast = null,
    BattleDamageEvent? Damage = null,
    BattleRestRecovery? Rest = null);

public sealed record BattleSpeechCue(string Text);

public sealed record BattleDamageEvent(
    int Amount,
    bool IsCritical = false,
    string? SourceUnitId = null);

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
    BuffRemoved,
    HooksTriggered,
    Healed,
    MpDamaged,
    RageChanged,
    SpeechRequested,
}
