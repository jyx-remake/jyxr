using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public interface IBattleEffectContext
{
    BattleState State { get; }
    HookTiming Timing { get; }
    BattleUnit Unit { get; }
    BattleUnit? Source { get; }
    BattleUnit? Target { get; }
    BattleBuffInstance? Buff { get; }
    SkillInstance? Skill { get; }
    IRandomService Random { get; }
    bool IsPreview { get; }
    BattleExecutionScope? ExecutionScope { get; }
    void RequestSpeech(BattleUnit speaker, string text);
    void RequestFloatText(BattleUnit target, string text, BattleFloatTextStyle style = BattleFloatTextStyle.Default);
}

public interface IBattleAbilityEffectContext
{
    BattleUnit Source { get; }
    SkillInstance Skill { get; }
    IReadOnlyList<BattleUnit> Targets { get; }
    IRandomService Random { get; }
    BattleExecutionScope? ExecutionScope { get; }

    int ApplyDirectDamage(BattleUnit target, int amount, string? detail = null);
}

public interface IDamageCalculationEffectContext : IBattleEffectContext
{
    BattleDamageCalculationContext DamageCalculation { get; }
}

public interface IHitResultEffectContext : IBattleEffectContext
{
    BattleHitState HitState { get; set; }
    int DamageAmount { get; set; }
    bool SuppressHitEffects { get; set; }
}

public interface IDamageApplicationEffectContext : IBattleEffectContext
{
    int DamageAmount { get; }
    void RedirectDamage(BattleUnit target, double damageFactor);
}

public interface IDamageTakenEffectContext : IBattleEffectContext
{
    int ActualDamageAmount { get; }
    bool IsCritical { get; }
}

public interface IRecoveryEffectContext : IBattleEffectContext
{
    BattleRecoveryKind RecoveryKind { get; }
    int RecoveryAmount { get; set; }
}

public interface ISkillCostEffectContext : IBattleEffectContext
{
    int MpCost { get; set; }
}

public interface IBuffApplicationEffectContext : IBattleEffectContext
{
    BattleBuffInstance AppliedBuff { get; }
    bool Cancel { get; set; }
}

public interface IActionStartEffectContext : IBattleEffectContext
{
    void SkipCurrentAction(string? reason = null);
}
