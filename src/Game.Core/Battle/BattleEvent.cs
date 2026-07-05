using Game.Core.Affix;
using Game.Core.Model.Skills;

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
    BattleRestRecovery? Rest = null,
    BattleSkillExperienceEvent? SkillExperience = null,
    BattleCharacterExperienceEvent? CharacterExperience = null);

public sealed record BattleSpeechCue(string Text);

public sealed record BattleDamageEvent(
    int Amount,
    bool IsCritical = false,
    string? SourceUnitId = null);

public sealed record BattleSkillExperienceEvent(
    string SkillId,
    string SkillName,
    SkillKind SkillKind,
    int AddedExperience,
    int OldLevel,
    int NewLevel);

public sealed record BattleCharacterExperienceEvent(
    string CharacterId,
    string CharacterName,
    int AddedExperience,
    int OldLevel,
    int NewLevel);

public enum BattleEventKind
{
    ActionStarted,
    ActionSkipped,
    Moved,
    MovementRolledBack,
    SkillCast,
    Damaged,
    ItemUsed,
    Rested,
    ActionEnded,
    BuffApplied,
    BuffResisted,
    BuffRemoved,
    HooksTriggered,
    Healed,
    MpDamaged,
    RageChanged,
    SpeechRequested,
    SkillLeveledUp,
    CharacterLeveledUp,
}
