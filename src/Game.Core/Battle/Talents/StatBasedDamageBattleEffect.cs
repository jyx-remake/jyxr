using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Adds a level-scaled amount of a source attribute after the normal damage
/// calculation.  Several legacy talents use this same pattern; keeping it as
/// a parameterized effect avoids one handler per talent.
/// </summary>
public sealed record StatBasedDamageBattleEffectParameters(
    [property: NotWhiteSpace] string SourceValue,
    [property: NonNegative] double ScalePerUnitLevel,
    [property: NonNegative] double FlatScale = 0d,
    [property: Probability] double Chance = 1d);

internal sealed class StatBasedDamageBattleEffectHandler
    : CustomBattleEffectHandler<StatBasedDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(StatBasedDamageBattleEffectParameters parameters)
    {
        if (parameters.SourceValue is not ("bili" or "dingli" or "fuyuan" or "gengu" or
            "jianfa" or "daofa" or "quanzhang" or "qimen" or "shenfa" or "wuxing" or "wuxue" or "mp"))
        {
            throw new InvalidOperationException(
                "Stat-based damage source must be a supported stat JSON name.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        StatBasedDamageBattleEffectParameters parameters)
    {
        if (!ReferenceEquals(context.Source, context.Unit) ||
            context.Skill?.Power is not > 0 ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var sourceValue = parameters.SourceValue == "mp"
            ? context.Unit.Mp
            : context.Unit.GetStat(parameters.SourceValue switch
            {
                "bili" => StatType.Bili,
                "dingli" => StatType.Dingli,
                "fuyuan" => StatType.Fuyuan,
                "gengu" => StatType.Gengu,
                "jianfa" => StatType.Jianfa,
                "daofa" => StatType.Daofa,
                "quanzhang" => StatType.Quanzhang,
                "qimen" => StatType.Qimen,
                "shenfa" => StatType.Shenfa,
                "wuxing" => StatType.Wuxing,
                "wuxue" => StatType.Wuxue,
                _ => throw new InvalidOperationException($"Unsupported stat '{parameters.SourceValue}'."),
            });
        var amount = Math.Floor(
            sourceValue * parameters.FlatScale +
            sourceValue * context.Unit.Character.Level * parameters.ScalePerUnitLevel);
        if (amount > 0d)
        {
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.FinalDamage,
                ModifierOp.PostAdd,
                amount);
        }
    }
}
