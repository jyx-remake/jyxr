using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record LifeAndDeathBattleEffectParameters(
    double Chance,
    double TargetMaxHpDamageFactor,
    double LifestealRate,
    string SourceSpeech,
    string TargetSpeech);

internal sealed class LifeAndDeathBattleEffectHandler
    : CustomBattleEffectHandler<LifeAndDeathBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(LifeAndDeathBattleEffectParameters parameters)
    {
        if (!double.IsFinite(parameters.Chance) || parameters.Chance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Life and death chance must be between 0 and 1.");
        }

        if (!double.IsFinite(parameters.TargetMaxHpDamageFactor) || parameters.TargetMaxHpDamageFactor < 0d)
        {
            throw new InvalidOperationException("Life and death target maximum HP damage factor must be non-negative.");
        }

        if (!double.IsFinite(parameters.LifestealRate) || parameters.LifestealRate < 0d)
        {
            throw new InvalidOperationException("Life and death lifesteal rate must be non-negative.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.SourceSpeech);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TargetSpeech);
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        LifeAndDeathBattleEffectParameters parameters)
    {
        var source = context.Source;
        var target = context.Target;
        if (source is null ||
            target is null ||
            !string.Equals(context.Unit.Id, source.Id, StringComparison.Ordinal) ||
            !context.State.AreEnemies(source, target) ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.PostAdd,
            target.MaxHp * parameters.TargetMaxHpDamageFactor);
        context.DamageCalculation.AdditionalLifestealRate += parameters.LifestealRate;

        context.RequestSpeech(target, BattleCueTextFormatter.Format(
            parameters.TargetSpeech,
            context.Unit,
            source,
            target)!);
        context.RequestSpeech(source, BattleCueTextFormatter.Format(
            parameters.SourceSpeech,
            context.Unit,
            source,
            target)!);
    }
}
