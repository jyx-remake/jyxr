using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Model;
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
    IHitConfirmedEffectContext,
    IDamageApplicationEffectContext,
    IDamageTakenEffectContext,
    IDamageDealtEffectContext,
    IDefeatPreventionEffectContext,
    IRecoveryEffectContext,
    ISkillCostEffectContext,
    IBuffApplicationEffectContext,
    IActionReadinessEffectContext,
    IActionStartEffectContext,
    IDamageApplicationRuntimeContext
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

    public int? IncomingDamageAmount { get; internal set; }

    public double? LifestealRate { get; internal set; }

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
    void IHitResultEffectContext.ResetUnitSkillCooldowns(string? detail) =>
        ResetUnitSkillCooldowns(detail);
    int IDamageTakenEffectContext.ActualDamageAmount =>
        DamageAmount ?? throw MissingCapability(nameof(IDamageTakenEffectContext));
    bool IDamageTakenEffectContext.IsCritical => IsCritical;
    int IDamageDealtEffectContext.ActualDamageAmount =>
        DamageAmount ?? throw MissingCapability(nameof(IDamageDealtEffectContext));
    bool IDamageDealtEffectContext.IsCritical => IsCritical;
    double IDamageDealtEffectContext.LifestealRate
    {
        get => LifestealRate ?? throw MissingCapability(nameof(IDamageDealtEffectContext));
        set => LifestealRate = value;
    }
    int IDefeatPreventionEffectContext.IncomingDamageAmount =>
        IncomingDamageAmount ?? throw MissingCapability(nameof(IDefeatPreventionEffectContext));
    int IDefeatPreventionEffectContext.ActualDamageAmount =>
        DamageAmount ?? throw MissingCapability(nameof(IDefeatPreventionEffectContext));
    bool IDefeatPreventionEffectContext.IsCritical => IsCritical;
    bool IDefeatPreventionEffectContext.IsDefeatPrevented => IsDefeatPrevented;
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

    public bool IsDefeatPrevented { get; private set; }

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

    public int SetRage(int value, string? detail = null)
    {
        if (Timing != HookTiming.BeforeActionStart)
        {
            throw new InvalidOperationException(
                $"Rage can only be set through an action-start effect during '{HookTiming.BeforeActionStart}'.");
        }

        return BattleResourceResolver.SetRage(State, Unit, value, Timing, detail);
    }

    public int RemoveNegativeBuffs()
    {
        if (Timing != HookTiming.BeforeActionReadiness)
        {
            throw new InvalidOperationException(
                $"Negative buffs can only be removed through an action-readiness effect during '{HookTiming.BeforeActionReadiness}'.");
        }

        return Engine.BuffResolver.Remove(
            State,
            Unit,
            Unit,
            static buff => buff.Definition.IsDebuff,
            Timing).Count;
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
        BattleFloatTextStyle style = BattleFloatTextStyle.Normal)
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

    public void CapDamage(int maximum)
    {
        if (Timing != HookTiming.BeforeDamageApplied)
        {
            throw new InvalidOperationException(
                $"Damage can only be capped during '{HookTiming.BeforeDamageApplied}'.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(maximum);
        DamageAmount = Math.Min(DamageAmount ?? 0, maximum);
    }

    public void CancelDamage(bool suppressHitEffects)
    {
        if (Timing != HookTiming.BeforeDamageApplied)
        {
            throw new InvalidOperationException(
                $"Damage can only be cancelled during '{HookTiming.BeforeDamageApplied}'.");
        }

        DamageAmount = 0;
        SuppressHitEffects |= suppressHitEffects;
    }

    private void ResetUnitSkillCooldowns(string? detail)
    {
        if (Timing != HookTiming.BeforeHitResolved)
        {
            throw new InvalidOperationException(
                $"Skill cooldowns can only be reset during '{HookTiming.BeforeHitResolved}'.");
        }

        Unit.Character.ResetSkillCooldowns();
        State.AddMessage(new BattleFact(
            BattleFactKind.SkillCooldownsReset,
            Unit.Id,
            Timing,
            detail: detail));
    }

    public void PreventDefeat(string abilityId)
    {
        if (Timing != HookTiming.BeforeDefeated)
        {
            throw new InvalidOperationException(
                $"Defeat can only be prevented during '{HookTiming.BeforeDefeated}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(abilityId);
        if (!Unit.IsAlive)
        {
            throw new InvalidOperationException(
                $"Unit '{Unit.Id}' must be alive before defeat can be prevented.");
        }

        if (IsDefeatPrevented)
        {
            throw new InvalidOperationException(
                $"Defeat has already been prevented for unit '{Unit.Id}'.");
        }

        IsDefeatPrevented = true;
        State.AddMessage(new BattleFact(
            BattleFactKind.DefeatPrevented,
            Unit.Id,
            Timing,
            detail: abilityId));
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
        State.AddMessage(new BattleFact(
            BattleFactKind.Healed,
            target.Id,
            Timing,
            detail: detail ?? restored.ToString()));
        return restored;
    }

    public int DamageMp(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        return BattleResourceResolver.DamageMp(State, target, amount, Timing, detail);
    }

    int IDamageApplicationRuntimeContext.ApplyDirectDamage(
        BattleUnit target,
        int amount,
        string? detail) => Damage(target, amount, detail);

    int IDamageApplicationRuntimeContext.ApplyHpRecovery(
        BattleUnit target,
        int amount,
        string? detail) => RestoreHp(target, amount, detail);

    int IActionStartEffectContext.ApplyHpRecovery(
        BattleUnit target,
        int amount,
        string? detail) => RestoreHp(target, amount, detail);

    bool IHitConfirmedEffectContext.IsCellAvailable(GridPosition position, BattleUnit movingUnit)
    {
        if (Timing != HookTiming.OnHitConfirmed)
        {
            throw new InvalidOperationException(
                $"Hit-confirmed relocation can only inspect cells during '{HookTiming.OnHitConfirmed}'.");
        }

        ArgumentNullException.ThrowIfNull(movingUnit);
        return State.Grid.IsWalkable(position) && !State.IsOccupied(position, movingUnit.Id);
    }

    bool IHitConfirmedEffectContext.TryRelocate(BattleUnit target, GridPosition destination)
    {
        if (Timing != HookTiming.OnHitConfirmed)
        {
            throw new InvalidOperationException(
                $"Hit-confirmed relocation can only occur during '{HookTiming.OnHitConfirmed}'.");
        }

        return Engine.TryRelocateByEffect(State, target, destination);
    }

    bool IHitConfirmedEffectContext.ApplyBuff(
        BattleUnit target,
        string buffId,
        int level,
        int duration)
    {
        if (Timing != HookTiming.OnHitConfirmed)
        {
            throw new InvalidOperationException(
                $"A hit-confirmed buff can only be applied during '{HookTiming.OnHitConfirmed}'.");
        }

        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(buffId);
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);

        var source = Source ?? Unit;
        return Engine.BuffResolver.Apply(
            State,
            source,
            target,
            Engine.BuffResolver.Resolve(buffId),
            level,
            duration,
            Timing);
    }

    int IDamageApplicationRuntimeContext.ApplyMpDamage(
        BattleUnit target,
        int amount,
        string? detail) => DamageMp(target, amount, detail);

}
