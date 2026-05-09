using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;
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
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        IRandomService random,
        BattleHookExecutionMode executionMode)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(random);

        State = state;
        Timing = timing;
        Unit = unit;
        Random = random;
        ExecutionMode = executionMode;
        BuffResolver = MissingBuffResolver;
    }

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

    public bool Cancel { get; set; }

    public Func<string, BuffDefinition> BuffResolver { get; set; }

    public void RequestSpeech(BattleUnit speaker, string text)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (!speaker.IsAlive)
        {
            return;
        }

        if (State.CurrentAction is not null &&
            !State.CurrentAction.TryRegisterSpeech(speaker.Id))
        {
            return;
        }

        State.AddEvent(new BattleEvent(
            BattleEventKind.SpeechRequested,
            speaker.Id,
            Timing,
            Speech: new BattleSpeechCue(text)));
    }

    public int Damage(BattleUnit target, int amount, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var damage = target.TakeDamage(amount);
        State.AddEvent(new BattleEvent(BattleEventKind.Damaged, target.Id, Detail: detail ?? damage.ToString()));
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

    private static BuffDefinition MissingBuffResolver(string buffId) =>
        throw new InvalidOperationException($"Battle hook requires buff resolver for '{buffId}'.");
}
