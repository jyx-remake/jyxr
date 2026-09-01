using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RandomHitBuffBattleEffectParameters(
    [property: Required, MinLength(1)] IReadOnlyList<string> BuffIds,
    [property: NonNegative] int Level,
    [property: Positive] int Duration,
    string Target = "source");

internal sealed class RandomHitBuffBattleEffectHandler
    : CustomBattleEffectHandler<RandomHitBuffBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(RandomHitBuffBattleEffectParameters parameters)
    {
        if (parameters.Target is not ("source" or "target"))
            throw new InvalidOperationException("Random hit buff target must be 'source' or 'target'.");
        if (parameters.BuffIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Random hit buff IDs cannot contain blank values.");
        if (parameters.BuffIds.Distinct(StringComparer.Ordinal).Count() != parameters.BuffIds.Count)
            throw new InvalidOperationException("Random hit buff IDs cannot contain duplicate values.");
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        RandomHitBuffBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !context.State.AreEnemies(context.Source, context.Target))
        {
            return;
        }

        var recipient = parameters.Target == "source" ? context.Source : context.Target;
        var buffId = parameters.BuffIds[context.Random.Next(0, parameters.BuffIds.Count)];
        context.ApplyBuff(recipient, buffId, parameters.Level, parameters.Duration);
    }
}
