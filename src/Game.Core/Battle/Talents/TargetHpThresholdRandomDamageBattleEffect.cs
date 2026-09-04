using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Skill-scoped random attack multiplier selected by the target HP threshold.
/// It also supports the legacy “legend skill is guaranteed critical” branch.
/// </summary>
public sealed record TargetHpThresholdRandomDamageBattleEffectParameters(
    [property: NotWhiteSpace] string SkillNameContains,
    [property: Probability] double Chance,
    [property: Probability] double TargetHpThreshold,
    [property: NonNegative] double HighMinimumFactor,
    [property: NonNegative] double HighMaximumFactor,
    [property: NonNegative] double LowMinimumFactor,
    [property: NonNegative] double LowMaximumFactor,
    bool CriticalOnLegend = false);

internal sealed class TargetHpThresholdRandomDamageBattleEffectHandler
    : CustomBattleEffectHandler<TargetHpThresholdRandomDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(TargetHpThresholdRandomDamageBattleEffectParameters parameters)
    {
        if (parameters.HighMinimumFactor > parameters.HighMaximumFactor ||
            parameters.LowMinimumFactor > parameters.LowMaximumFactor)
        {
            throw new InvalidOperationException("Random damage factor range is invalid.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        TargetHpThresholdRandomDamageBattleEffectParameters parameters)
    {
        if (!ReferenceEquals(context.Source, context.Unit) ||
            context.Target is null ||
            context.Skill is null ||
            !context.Skill.Name.Contains(parameters.SkillNameContains, StringComparison.Ordinal) ||
            context.Target.MaxHp <= 0 ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var targetHpRatio = Math.Clamp((double)context.Target.Hp / context.Target.MaxHp, 0d, 1d);
        var high = targetHpRatio >= parameters.TargetHpThreshold;
        var minimum = high ? parameters.HighMinimumFactor : parameters.LowMinimumFactor;
        var maximum = high ? parameters.HighMaximumFactor : parameters.LowMaximumFactor;
        var factor = minimum + context.Random.NextDouble() * (maximum - minimum);
        TalentDamageModifier.MultiplyAttack(context, factor);

        if (parameters.CriticalOnLegend && context.Skill.SkillKind == SkillKind.Legend)
        {
            context.DamageCalculation.AddModifier(
                BattleDamageContextField.CriticalChance,
                ModifierOp.Override,
                1d);
        }
    }
}
