using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Core.Battle;

public static class BattleRestCalculator
{
    private const double HpRecoveryBase = 40d;
    private const double MpRecoveryBase = 60d;
    private const double HpGenguScale = 1.5d;
    private const double MpGenguScale = 2d;
    private const double LegacyInternalBase = 100d;
    private const double LegacyInternalDivisor = 150d;
    private const double HpRollLow = 0.1d;
    private const double HpRollHigh = 2d;
    private const double MpRollLow = 0.2d;
    private const double MpRollHigh = 2d;

    public static BattleRestRecovery Roll(BattleUnit unit, IRandomService random)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(random);

        return Calculate(
            unit,
            Roll(random, HpRollLow, HpRollHigh),
            Roll(random, MpRollLow, MpRollHigh));
    }

    public static BattleRestRecovery EstimateAverage(BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        return Calculate(
            unit,
            (HpRollLow + HpRollHigh) / 2d,
            (MpRollLow + MpRollHigh) / 2d);
    }

    private static BattleRestRecovery Calculate(BattleUnit unit, double hpRoll, double mpRoll)
    {
        var gengu = unit.GetStat(StatType.Gengu);
        var internalFactor = (ResolveEquippedInternalSkillPower(unit) + LegacyInternalBase) / LegacyInternalDivisor;

        var hpRecovery = ResolveRecovery(
            HpRecoveryBase,
            HpGenguScale,
            gengu,
            internalFactor,
            hpRoll);
        var mpRecovery = ResolveRecovery(
            MpRecoveryBase,
            MpGenguScale,
            gengu,
            internalFactor,
            mpRoll);

        return new BattleRestRecovery(
            Math.Min(hpRecovery, Math.Max(0, unit.MaxHp - unit.Hp)),
            Math.Min(mpRecovery, Math.Max(0, unit.MaxMp - unit.Mp)));
    }

    private static int ResolveRecovery(
        double recoveryBase,
        double genguScale,
        double gengu,
        double internalFactor,
        double roll)
    {
        var recovery = (int)(recoveryBase * (1d + genguScale * gengu / 100d) * internalFactor * roll);
        return recovery == 0 ? 1 : recovery;
    }

    private static int ResolveEquippedInternalSkillPower(BattleUnit unit)
    {
        var equippedInternalSkill = unit.Character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped);
        return equippedInternalSkill is null
            ? 0
            : Math.Max(equippedInternalSkill.Yin, Math.Abs(equippedInternalSkill.Yang));
    }

    private static double Roll(IRandomService random, double lowInclusive, double highExclusive) =>
        lowInclusive + (highExclusive - lowInclusive) * random.NextDouble();
}

public readonly record struct BattleRestRecovery(int Hp, int Mp);
