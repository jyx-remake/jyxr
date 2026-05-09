using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class BattleDamageCalculator
{
    private readonly IRandomService _random;

    public BattleDamageCalculator(IRandomService? random = null)
    {
        _random = random ?? SharedRandomService.Instance;
    }

    public BattleDamageResult CalculateSkillDamage(BattleDamageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CalculateSkillDamage(CreateSkillDamageContext(context));
    }

    public BattleDamageCalculationContext CreateSkillDamageContext(BattleDamageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var damageContext = new BattleDamageCalculationContext(context.Source, context.Target, context.Skill);
        if (context.Skill.Power <= 0)
        {
            return damageContext;
        }

        var source = context.Source;
        var target = context.Target;
        var sourceCharacter = source.Character;
        var sourceInternalSkill = GetEquippedInternalSkill(sourceCharacter);
        var targetInternalSkill = GetEquippedInternalSkill(target.Character);

        var skillTypeStat = ResolveSkillTypeStat(context.Skill.WeaponType);
        var skillTypeValue = source.GetStat(skillTypeStat);
        var internalAffinity = ResolveInternalAffinity(context.Skill, sourceInternalSkill);
        var attackBase = context.Skill.Power
            * (2d + skillTypeValue / 200d)
            * 2.5d
            * (4d + source.GetStat(StatType.Bili) / 120d)
            * (1d + internalAffinity);

        var weaponBonusMultiplier = 1d + source.GetWeaponBonusValue(context.Skill.WeaponType, 0d);
        var attackLow = attackBase * weaponBonusMultiplier + source.GetStat(StatType.Attack) / 2d;
        var attackHigh = attackBase
            * (1d + (sourceInternalSkill?.AttackRatio ?? 0d))
            * weaponBonusMultiplier
            + source.GetStat(StatType.Attack);
        if (attackHigh < attackLow)
        {
            (attackLow, attackHigh) = (attackHigh, attackLow);
        }

        var criticalChance = source.GetStat(StatType.Fuyuan) / 1000d
            * (1d + (sourceInternalSkill?.CriticalRatio ?? 0d))
            * (1d + internalAffinity)
            + source.GetStat(StatType.CritChance)
            - target.GetStat(StatType.AntiCritChance);
        var defence = 150d
            + (10d + target.GetStat(StatType.Dingli) / 40d + target.GetStat(StatType.Gengu) / 70d)
            * 8d
            * (1d + (targetInternalSkill?.DefenceRatio ?? 0d))
            + target.GetStat(StatType.Defence);

        damageContext.AttackLow = attackLow;
        damageContext.AttackHigh = attackHigh;
        damageContext.CriticalChance = criticalChance;
        damageContext.CriticalMultiplier = 1.5d + source.GetStat(StatType.CritMult);
        damageContext.Defence = defence;
        return damageContext;
    }

    public BattleDamageResult CalculateSkillDamage(BattleDamageCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Skill.Power <= 0)
        {
            return new BattleDamageResult(0, IsCritical: false, CriticalChance: 0d, DefenceReduction: 0d);
        }

        var attackLow = context.Evaluate(
            BattleDamageContextField.SourceAttackLow,
            context.Evaluate(BattleDamageContextField.SourceAttack, context.AttackLow));
        var attackHigh = context.Evaluate(
            BattleDamageContextField.SourceAttackHigh,
            context.Evaluate(BattleDamageContextField.SourceAttack, context.AttackHigh));
        if (attackHigh < attackLow)
        {
            (attackLow, attackHigh) = (attackHigh, attackLow);
        }

        var criticalChance = Math.Clamp(
            context.Evaluate(BattleDamageContextField.CriticalChance, context.CriticalChance),
            0d,
            1d);
        var criticalMultiplier = Math.Max(
            1d,
            context.Evaluate(BattleDamageContextField.CriticalMultiplier, context.CriticalMultiplier));
        var defence = Math.Max(0d, context.Evaluate(BattleDamageContextField.TargetDefence, context.Defence));
        var isCritical = _random.RollChance(criticalChance);
        var rolledAttack = Roll(attackLow, attackHigh);
        var defenceReduction = Math.Clamp(CalculateDefenceReduction(defence), 0d, 0.9d);
        var baseAmount = rolledAttack * (isCritical ? criticalMultiplier : 1d) * (1d - defenceReduction);
        var amount = (int)context.Evaluate(BattleDamageContextField.FinalDamage, baseAmount);

        return new BattleDamageResult(
            Math.Max(0, amount),
            isCritical,
            criticalChance,
            defenceReduction);
    }

    public BattleDamageResult EstimateSkillDamage(BattleDamageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return EstimateSkillDamage(CreateSkillDamageContext(context));
    }

    public BattleDamageResult EstimateSkillDamage(BattleDamageCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Skill.Power <= 0)
        {
            return new BattleDamageResult(0, IsCritical: false, CriticalChance: 0d, DefenceReduction: 0d);
        }

        var attackLow = context.Evaluate(
            BattleDamageContextField.SourceAttackLow,
            context.Evaluate(BattleDamageContextField.SourceAttack, context.AttackLow));
        var attackHigh = context.Evaluate(
            BattleDamageContextField.SourceAttackHigh,
            context.Evaluate(BattleDamageContextField.SourceAttack, context.AttackHigh));
        if (attackHigh < attackLow)
        {
            (attackLow, attackHigh) = (attackHigh, attackLow);
        }

        var criticalChance = Math.Clamp(
            context.Evaluate(BattleDamageContextField.CriticalChance, context.CriticalChance),
            0d,
            1d);
        var criticalMultiplier = Math.Max(
            1d,
            context.Evaluate(BattleDamageContextField.CriticalMultiplier, context.CriticalMultiplier));
        var defence = Math.Max(0d, context.Evaluate(BattleDamageContextField.TargetDefence, context.Defence));
        var averagedAttack = (attackLow + attackHigh) / 2d;
        var defenceReduction = Math.Clamp(CalculateDefenceReduction(defence), 0d, 0.9d);
        var baseAmount = averagedAttack * (1d + criticalChance * (criticalMultiplier - 1d)) * (1d - defenceReduction);
        var amount = (int)context.Evaluate(BattleDamageContextField.FinalDamage, baseAmount);

        return new BattleDamageResult(
            Math.Max(0, amount),
            IsCritical: false,
            criticalChance,
            defenceReduction);
    }

    public static double CalculateDefenceReduction(double defence) =>
        0.9d - Math.Pow(0.9d, 0.02d * (defence + 50d));

    private double Roll(double lowInclusive, double highInclusive)
    {
        if (Math.Abs(highInclusive - lowInclusive) < double.Epsilon)
        {
            return lowInclusive;
        }

        return lowInclusive + (highInclusive - lowInclusive) * _random.NextDouble();
    }

    private static InternalSkillInstance? GetEquippedInternalSkill(CharacterInstance character) =>
        character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped);

    private static double ResolveInternalAffinity(SkillInstance skill, InternalSkillInstance? internalSkill)
    {
        if (internalSkill is null)
        {
            return 0d;
        }

        if (skill.IsHarmony)
        {
            return Math.Max(internalSkill.Yin, internalSkill.Yang) / 100d;
        }

        return skill.Affinity switch
        {
            > 0d => skill.Affinity * internalSkill.Yang / 100d,
            < 0d => -skill.Affinity * internalSkill.Yin / 100d,
            _ => 0d,
        };
    }

    private static StatType ResolveSkillTypeStat(WeaponType weaponType) =>
        weaponType switch
        {
            WeaponType.Quanzhang => StatType.Quanzhang,
            WeaponType.Jianfa => StatType.Jianfa,
            WeaponType.Daofa => StatType.Daofa,
            WeaponType.Qimen => StatType.Qimen,
            WeaponType.InternalSkill => StatType.Gengu,
            _ => throw new InvalidOperationException($"Unsupported skill weapon type '{weaponType}'."),
        };

    private sealed class SharedRandomService : IRandomService
    {
        public static SharedRandomService Instance { get; } = new();

        public double NextDouble() => Random.Shared.NextDouble();

        public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
    }
}

public sealed record BattleDamageContext(
    BattleUnit Source,
    BattleUnit Target,
    SkillInstance Skill);

public sealed record BattleDamageResult(
    int Amount,
    bool IsCritical,
    double CriticalChance,
    double DefenceReduction);
