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

    public BattleUnit? Source { get; internal set; }

    public BattleUnit? Target { get; internal set; }

    public BattleBuffInstance? Buff { get; internal set; }

    public SkillInstance? Skill { get; internal set; }

    public BattleDamageCalculationContext? DamageCalculation { get; internal set; }

    public int? MpCost { get; internal set; }

    public int? DamageAmount { get; internal set; }

    public BattleRecoveryKind? RecoveryKind { get; internal set; }

    public int? RecoveryAmount { get; internal set; }

    public bool IsCritical { get; internal set; }

    public BattleHitState HitState { get; internal set; } = BattleHitState.Hit;

    public bool SuppressHitEffects { get; internal set; }

    public bool Cancel { get; internal set; }

    public bool IsActionSkipRequested { get; private set; }

    public string? ActionSkipReason { get; private set; }

    public void SkipCurrentAction(string? reason = null)
    {
        if (Timing != HookTiming.BeforeActionStart)
        {
            throw new InvalidOperationException(
                $"An action can only be skipped during '{HookTiming.BeforeActionStart}'.");
        }

        IsActionSkipRequested = true;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            ActionSkipReason ??= reason;
        }
    }

    public void RequestSpeech(BattleUnit speaker, string text)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        BattleSpeechRuntime.TryEmit(State, speaker, text, Timing);
    }

    public void RequestFloatText(
        BattleUnit target,
        string text,
        BattleFloatTextStyle style = BattleFloatTextStyle.Default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        State.AddEvent(new BattleEvent(
            BattleEventKind.FloatTextRequested,
            target.Id,
            Timing,
            FloatText: new BattleFloatTextCue(text, style)));
    }

    public void RedirectDamage(BattleUnit target, double damageFactor)
    {
        if (Timing != HookTiming.BeforeDamageApplied)
        {
            throw new InvalidOperationException(
                $"Damage can only be redirected during '{HookTiming.BeforeDamageApplied}'.");
        }

        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(damageFactor);
        Target = target;
        DamageAmount = Math.Max(0, (int)((DamageAmount ?? 0) * damageFactor));
    }

    public int Damage(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        return Engine.ApplyDirectDamage(
            State,
            Source ?? Unit,
            target,
            amount,
            Timing,
            detail);
    }

    public int RestoreHp(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var source = Source ?? Unit;
        var restored = Engine.RestoreHookRecovery(
            State,
            source,
            target,
            BattleRecoveryKind.Hp,
            amount);
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
