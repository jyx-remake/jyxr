using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public enum BattleHookExecutionMode
{
    Execute,
    Preview,
}

public sealed class BattleHookContext :
    IBattleEffectContext,
    IDamageCalculationEffectContext,
    IHitResultEffectContext,
    IDamageApplicationEffectContext,
    IDamageTakenEffectContext,
    IRecoveryEffectContext,
    ISkillCostEffectContext,
    IBuffApplicationEffectContext,
    IActionStartEffectContext
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

    public BattleExecutionScope? ExecutionScope => State.CurrentExecutionScope;

    public BattleUnit? Source { get; internal set; }

    public BattleUnit? Target { get; internal set; }

    public BattleBuffInstance? Buff { get; internal set; }

    public SkillInstance? Skill { get; internal set; }

    public BattleDamageCalculationContext? DamageCalculation { get; internal set; }

    public int? MpCost { get; internal set; }

    public int? DamageAmount { get; internal set; }

    BattleDamageCalculationContext IDamageCalculationEffectContext.DamageCalculation =>
        DamageCalculation ?? throw MissingCapability(nameof(IDamageCalculationEffectContext));

    int IDamageApplicationEffectContext.DamageAmount =>
        DamageAmount ?? throw MissingCapability(nameof(IDamageApplicationEffectContext));

    BattleHitState IHitResultEffectContext.HitState { get => HitState; set => HitState = value; }
    int IHitResultEffectContext.DamageAmount
    {
        get => DamageAmount ?? throw MissingCapability(nameof(IHitResultEffectContext));
        set => DamageAmount = value;
    }
    bool IHitResultEffectContext.SuppressHitEffects
    {
        get => SuppressHitEffects;
        set => SuppressHitEffects = value;
    }
    int IDamageTakenEffectContext.ActualDamageAmount =>
        DamageAmount ?? throw MissingCapability(nameof(IDamageTakenEffectContext));
    bool IDamageTakenEffectContext.IsCritical => IsCritical;
    BattleRecoveryKind IRecoveryEffectContext.RecoveryKind =>
        RecoveryKind ?? throw MissingCapability(nameof(IRecoveryEffectContext));
    int IRecoveryEffectContext.RecoveryAmount
    {
        get => RecoveryAmount ?? throw MissingCapability(nameof(IRecoveryEffectContext));
        set => RecoveryAmount = value;
    }
    int ISkillCostEffectContext.MpCost
    {
        get => MpCost ?? throw MissingCapability(nameof(ISkillCostEffectContext));
        set => MpCost = value;
    }
    BattleBuffInstance IBuffApplicationEffectContext.AppliedBuff =>
        Buff ?? throw MissingCapability(nameof(IBuffApplicationEffectContext));
    bool IBuffApplicationEffectContext.Cancel { get => Cancel; set => Cancel = value; }

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
        State.AddMessage(new BattleCue(
            BattleCueKind.FloatTextRequested,
            target.Id,
            Timing,
            floatText: new BattleFloatTextCue(text, style)));
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

    private InvalidOperationException MissingCapability(string capability) =>
        new($"Timing '{Timing}' did not provide required capability '{capability}'.");

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
        var restored = Engine.RecoveryResolver.Apply(
            State,
            source,
            target,
            BattleRecoveryKind.Hp,
            amount).ActualAmount;
        State.AddMessage(new BattleFact(BattleFactKind.Healed, target.Id, detail: detail ?? restored.ToString()));
        return restored;
    }

    public int DamageMp(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var drained = target.DamageMp(amount);
        State.AddMessage(new BattleFact(BattleFactKind.MpDamaged, target.Id, detail: detail ?? drained.ToString()));
        return drained;
    }

}
