using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Multiplies attack by a function of the target's current HP ratio. The
/// denominator form is kept explicit because it mirrors the legacy Lua
/// formula used by 骑士之心: 1 / (1 + missingHpRatio).
/// </summary>
public sealed record TargetHpRatioDamageBattleEffectParameters(
    [property: Positive] double Denominator,
    [property: NonNegative] double RatioCoefficient = 1d);

internal sealed class TargetHpRatioDamageBattleEffectHandler
    : CustomBattleEffectHandler<TargetHpRatioDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        TargetHpRatioDamageBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !ReferenceEquals(context.Source, context.Unit) ||
            context.Skill?.Power is not > 0 ||
            context.Target.MaxHp <= 0)
        {
            return;
        }

        var targetHpRatio = Math.Clamp((double)context.Target.Hp / context.Target.MaxHp, 0d, 1d);
        var denominator = parameters.Denominator - parameters.RatioCoefficient * targetHpRatio;
        if (denominator <= 0d)
        {
            return;
        }

        TalentDamageModifier.MultiplyAttack(context, 1d / denominator);
    }
}
