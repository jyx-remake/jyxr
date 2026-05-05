using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class CurrencyState
{
    public int Silver { get; private set; }

    public int Gold { get; private set; }

    public static CurrencyState Restore(CurrencyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentOutOfRangeException.ThrowIfNegative(record.Silver);
        ArgumentOutOfRangeException.ThrowIfNegative(record.Gold);

        return new CurrencyState
        {
            Silver = record.Silver,
            Gold = record.Gold,
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

    public void ChangeGold(int delta)
    {
        if (delta >= 0)
        {
            AddGold(delta);
            return;
        }

        ArgumentOutOfRangeException.ThrowIfEqual(delta, int.MinValue);

        SpendGold(-delta);
    }

    public void AddSilver(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Silver = checked(Silver + amount);
    }

    public void AddGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Gold = checked(Gold + amount);
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

    public void SpendGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!CanSpendGold(amount))
        {
            throw new InvalidOperationException("Not enough gold.");
        }

        Gold -= amount;
    }

    public bool CanSpendSilver(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return Silver >= amount;
    }

    public bool CanSpendGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return Gold >= amount;
    }

    public CurrencyRecord ToRecord() => new(Silver, Gold);
}
