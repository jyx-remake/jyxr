using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public enum BattleHookExecutionMode
{
    Execute,
    Preview,
}

public sealed class BattleHookContext
{
    internal BattleHookContext(
        BattleEngine engine,
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        IRandomService random,
        BattleHookExecutionMode executionMode)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(random);

        Engine = engine;
        State = state;
        Timing = timing;
        Unit = unit;
        Random = random;
        ExecutionMode = executionMode;
    }

    internal BattleEngine Engine { get; }

    public BattleState State { get; }

    public HookTiming Timing { get; }

    public BattleUnit Unit { get; }

    public IRandomService Random { get; }

    public BattleHookExecutionMode ExecutionMode { get; }

    public bool IsPreview => ExecutionMode == BattleHookExecutionMode.Preview;

    public BattleUnit? Source { get; set; }

    public BattleUnit? Target { get; set; }

    public BattleBuffInstance? Buff { get; set; }

    public SkillInstance? Skill { get; set; }

    public BattleDamageCalculationContext? DamageCalculation { get; set; }

    public int? MpCost { get; set; }

    public int? DamageAmount { get; set; }

    public BattleHitState HitState { get; set; } = BattleHitState.Hit;

    public bool HitCancelled
    {
        get => HitState == BattleHitState.Miss;
        set => HitState = value ? BattleHitState.Miss : BattleHitState.Hit;
    }

    public bool SuppressHitEffects { get; set; }

    public bool Cancel { get; set; }

    public void RequestSpeech(BattleUnit speaker, string text)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        BattleSpeechRuntime.TryEmit(State, speaker, text, Timing);
    }

    public int Damage(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var damage = target.TakeDamage(amount);
        State.AddEvent(new BattleEvent(
            BattleEventKind.Damaged,
            target.Id,
            Timing,
            Detail: detail,
            Damage: new BattleDamageEvent(damage, SourceUnitId: Source?.Id)));
        return damage;
    }

    public int RestoreHp(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var restored = target.RestoreHp(amount);
        State.AddEvent(new BattleEvent(BattleEventKind.Healed, target.Id, Detail: detail ?? restored.ToString()));
        return restored;
    }

    public int DamageMp(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var drained = target.DamageMp(amount);
        State.AddEvent(new BattleEvent(BattleEventKind.MpDamaged, target.Id, Detail: detail ?? drained.ToString()));
        return drained;
    }

}
