using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class CurrencyState
{
    public int Silver { get; private set; }

    public static CurrencyState Restore(CurrencyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentOutOfRangeException.ThrowIfNegative(record.Silver);

        return new CurrencyState
        {
            Silver = record.Silver,
        };
    }

    public void ChangeSilver(int delta)
    {
        if (delta >= 0)
        {
            AddSilver(delta);
            return;
        }

        ArgumentOutOfRangeException.ThrowIfEqual(delta, int.MinValue);

        SpendSilver(-delta);
    }

    public void AddSilver(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Silver = checked(Silver + amount);
    }

    public void SpendSilver(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!CanSpendSilver(amount))
        {
            throw new InvalidOperationException("Not enough silver.");
        }

        Silver -= amount;
    }

    public bool CanSpendSilver(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return Silver >= amount;
    }

    public CurrencyRecord ToRecord() => new(Silver);
}
