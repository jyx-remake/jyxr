using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RandomDebuffBattleEffectParameters(
    [property: Required, MinLength(1)] IReadOnlyList<string> BuffIds,
    [property: NonNegative] int Level,
    [property: Positive] int Duration);

internal sealed class RandomDebuffBattleEffectHandler
    : CustomBattleEffectHandler<RandomDebuffBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(RandomDebuffBattleEffectParameters parameters)
    {
        if (parameters.BuffIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Random debuff IDs cannot contain blank values.");
        if (parameters.BuffIds.Distinct(StringComparer.Ordinal).Count() != parameters.BuffIds.Count)
            throw new InvalidOperationException("Random debuff IDs cannot contain duplicate values.");
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        RandomDebuffBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !context.State.AreEnemies(context.Source, context.Target))
        {
            return;
        }

        var buffId = parameters.BuffIds[context.Random.Next(0, parameters.BuffIds.Count)];
        context.ApplyBuff(context.Target, buffId, parameters.Level, parameters.Duration);
    }
}
