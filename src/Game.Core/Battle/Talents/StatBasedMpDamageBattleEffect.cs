using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Applies MP damage derived from a source stat and character level.
/// This covers legacy on-hit branches whose Lua implementation calls DamageMp.
/// </summary>
public sealed record StatBasedMpDamageBattleEffectParameters(
    [property: NotWhiteSpace] string SourceValue,
    [property: NonNegative] double ScalePerUnitLevel,
    [property: NonNegative] double FlatScale = 0d,
    [property: Probability] double Chance = 1d);

internal sealed class StatBasedMpDamageBattleEffectHandler
    : CustomBattleEffectHandler<StatBasedMpDamageBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(StatBasedMpDamageBattleEffectParameters parameters)
    {
        if (parameters.SourceValue is not ("bili" or "dingli" or "fuyuan" or "gengu" or
            "jianfa" or "daofa" or "quanzhang" or "qimen" or "shenfa" or "wuxing" or "wuxue" or "mp"))
        {
            throw new InvalidOperationException(
                "Stat-based MP damage source must be a supported stat JSON name.");
        }
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        StatBasedMpDamageBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            context.Source is null ||
            context.Target is null ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var sourceValue = parameters.SourceValue == "mp"
            ? context.Source.Mp
            : context.Source.GetStat(parameters.SourceValue switch
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
            sourceValue * context.Source.Character.Level * parameters.ScalePerUnitLevel);
        if (amount > 0d)
        {
            hookContext.DamageMp(context.Target, (int)amount);
        }
    }
}
