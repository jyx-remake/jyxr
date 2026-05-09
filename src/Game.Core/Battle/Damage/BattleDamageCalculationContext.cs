using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class BattleDamageCalculationContext
{
    private readonly Dictionary<BattleDamageContextField, BattleDamageModifierBucket> _modifiers = [];

    public BattleDamageCalculationContext(BattleUnit source, BattleUnit target, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(skill);

        Source = source;
        Target = target;
        Skill = skill;
    }

    public BattleUnit Source { get; }

    public BattleUnit Target { get; }

    public SkillInstance Skill { get; }

    public double AttackLow { get; set; }

    public double AttackHigh { get; set; }

    public double CriticalChance { get; set; }

    public double CriticalMultiplier { get; set; } = 1d;

    public double Defence { get; set; }

    public void AddModifier(BattleDamageContextField field, ModifierOp op, double value)
    {
        var bucket = _modifiers.TryGetValue(field, out var existing)
            ? existing
            : BattleDamageModifierBucket.Empty;
        bucket = bucket.Apply(op, value);
        _modifiers[field] = bucket;
    }

    public double Evaluate(BattleDamageContextField field, double baseValue) =>
        _modifiers.TryGetValue(field, out var bucket) ? bucket.Evaluate(baseValue) : baseValue;

    private readonly record struct BattleDamageModifierBucket(double Add, double Increase, double Multiplier, double PostAdd)
    {
        public static BattleDamageModifierBucket Empty => new(0d, 0d, 1d, 0d);

        public BattleDamageModifierBucket Apply(ModifierOp op, double value) =>
            op switch
            {
                ModifierOp.Add => this with { Add = Add + value },
                ModifierOp.Increase => this with { Increase = Increase + value },
                ModifierOp.More => this with { Multiplier = Multiplier * value },
                ModifierOp.PostAdd => this with { PostAdd = PostAdd + value },
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };

        public double Evaluate(double baseValue) => (baseValue + Add) * (1d + Increase) * Multiplier + PostAdd;
    }
}
