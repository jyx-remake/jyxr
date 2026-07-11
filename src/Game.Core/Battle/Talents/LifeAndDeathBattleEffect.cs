using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record LifeAndDeathBattleEffectParameters(
    [property: Probability] double Chance,
    [property: NonNegative] double TargetMaxHpDamageFactor,
    [property: NonNegative] double LifestealRate,
    [property: NotWhiteSpace] string SourceSpeech,
    [property: NotWhiteSpace] string TargetSpeech);

internal sealed class LifeAndDeathBattleEffectHandler
    : CustomBattleEffectHandler<LifeAndDeathBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

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
