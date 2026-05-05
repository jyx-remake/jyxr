using Game.Core.Affix;

namespace Game.Core;

public readonly record struct ModifierBucket(double Add, double Increase, double Multiplier, double PostAdd)
{
    public static ModifierBucket Empty => new(0d, 0d, 1d, 0d);

    public ModifierBucket Apply(ModifierValue value) =>
        value.Op switch
        {
            ModifierOp.Add => this with { Add = Add + value.Delta },
            ModifierOp.Increase => this with { Increase = Increase + value.Delta },
            ModifierOp.More => this with { Multiplier = Multiplier * value.Delta },
            ModifierOp.PostAdd => this with { PostAdd = PostAdd + value.Delta },
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };

    public ModifierBucket Combine(ModifierBucket other) => new(
        Add + other.Add,
        Increase + other.Increase,
        Multiplier * other.Multiplier,
        PostAdd + other.PostAdd);

    public double Evaluate(double baseValue) => (baseValue + Add) * (1d + Increase) * Multiplier + PostAdd;
}
