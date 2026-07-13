using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record BelovedCompanionDamageBattleEffectParameters(
    [property: NotWhiteSpace] string BeneficiaryName,
    [property: NotWhiteSpace] string CompanionName,
    [property: NonNegative] double MinimumDamageFactor,
    [property: NonNegative] double MaximumDamageFactor,
    [property: Probability] double SpeechChance = 0d,
    IReadOnlyList<string>? SpeechLines = null);

internal sealed class BelovedCompanionDamageBattleEffectHandler
    : CustomBattleEffectHandler<BelovedCompanionDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(BelovedCompanionDamageBattleEffectParameters parameters)
    {
        if (parameters.MinimumDamageFactor > parameters.MaximumDamageFactor)
        {
            throw new InvalidOperationException(
                "Beloved companion minimum damage factor cannot exceed its maximum damage factor.");
        }

        if (parameters.SpeechLines?.Any(string.IsNullOrWhiteSpace) == true)
        {
            throw new InvalidOperationException("Beloved companion speech lines cannot contain blank values.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        BelovedCompanionDamageBattleEffectParameters parameters)
    {
        if (!string.Equals(
                context.Unit.Character.Definition.Name,
                parameters.BeneficiaryName,
                StringComparison.Ordinal) ||
            !context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Unit.Team &&
                string.Equals(
                    unit.Character.Definition.Name,
                    parameters.CompanionName,
                    StringComparison.Ordinal)))
        {
            return;
        }

        var damageFactor = parameters.MinimumDamageFactor +
            context.Random.NextDouble() * (parameters.MaximumDamageFactor - parameters.MinimumDamageFactor);
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.More,
            damageFactor);

        if (parameters.SpeechLines is not { Count: > 0 })
        {
            return;
        }

        var speech = BattleSpeechRuntime.TryPickLine(
            new BattleSpeechDefinition
            {
                Lines = parameters.SpeechLines,
                Chance = parameters.SpeechChance,
            },
            context.Random);
        if (speech is not null)
        {
            context.RequestSpeech(context.Unit, speech);
        }
    }
}
