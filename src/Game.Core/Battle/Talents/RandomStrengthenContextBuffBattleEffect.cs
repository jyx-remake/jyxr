using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RandomStrengthenContextBuffBattleEffectParameters(
    [property: Probability] double Chance,
    [property: NonNegative] int MinimumLevelDelta,
    [property: NonNegative] int MaximumLevelDelta,
    [property: NonNegative] int MinimumTurnDelta,
    [property: NonNegative] int MaximumTurnDelta);

internal sealed class RandomStrengthenContextBuffBattleEffectHandler
    : CustomBattleEffectHandler<RandomStrengthenContextBuffBattleEffectParameters, IBuffApplicationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeBuffApplied };

    public override void Validate(RandomStrengthenContextBuffBattleEffectParameters parameters)
    {
        if (parameters.MinimumLevelDelta > parameters.MaximumLevelDelta)
            throw new InvalidOperationException("Minimum buff level delta cannot exceed the maximum delta.");
        if (parameters.MinimumTurnDelta > parameters.MaximumTurnDelta)
            throw new InvalidOperationException("Minimum buff turn delta cannot exceed the maximum delta.");
    }

    public override void Execute(
        IBuffApplicationEffectContext context,
        RandomStrengthenContextBuffBattleEffectParameters parameters)
    {
        if (!context.AppliedBuff.Definition.IsDebuff ||
            !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        var levelDelta = context.Random.Next(
            parameters.MinimumLevelDelta,
            checked(parameters.MaximumLevelDelta + 1));
        var turnDelta = context.Random.Next(
            parameters.MinimumTurnDelta,
            checked(parameters.MaximumTurnDelta + 1));
        context.AppliedBuff.Strengthen(levelDelta, turnDelta);
    }
}
