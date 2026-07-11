using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record CloudClimbingDragonBattleEffectParameters(
    [property: Probability] double Chance,
    [property: NonNegative] double MinimumShenfaFactor,
    [property: NonNegative] double MaximumShenfaFactor,
    [property: Required, MinLength(1)] IReadOnlyList<string> SpeechLines,
    [property: Probability] double SpeechChance);

internal sealed class CloudClimbingDragonBattleEffectHandler
    : CustomBattleEffectHandler<CloudClimbingDragonBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(CloudClimbingDragonBattleEffectParameters parameters)
    {
        if (parameters.MaximumShenfaFactor < parameters.MinimumShenfaFactor)
        {
            throw new InvalidOperationException(
                "Cloud climbing dragon maximum Shenfa factor cannot be lower than its minimum factor.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        CloudClimbingDragonBattleEffectParameters parameters)
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

        var shenfa = Math.Max(0d, source.GetStat(StatType.Shenfa));
        var minimum = (int)Math.Floor(shenfa * parameters.MinimumShenfaFactor);
        var maximum = Math.Max(minimum, (int)Math.Floor(shenfa * parameters.MaximumShenfaFactor));
        var additionalDamage = context.Random.Next(minimum, maximum + 1);
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.PostAdd,
            additionalDamage);

        var speech = BattleSpeechRuntime.TryPickLine(
            new BattleSpeechDefinition
            {
                Lines = parameters.SpeechLines,
                Chance = parameters.SpeechChance,
            },
            context.Random);
        if (speech is not null)
        {
            context.RequestSpeech(source, speech);
        }
    }
}
