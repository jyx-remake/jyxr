using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record AttributeContestDebuffBattleEffectParameters(
    StatType SourceStat,
    StatType TargetStat,
    [property: NonNegative] double Scale,
    [property: NotWhiteSpace] string BuffId,
    [property: NonNegative] int Level,
    [property: Positive] int Duration,
    [property: Probability] double MinimumChance = 0d,
    [property: Probability] double MaximumChance = 1d);

internal sealed class AttributeContestDebuffBattleEffectHandler
    : CustomBattleEffectHandler<AttributeContestDebuffBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(AttributeContestDebuffBattleEffectParameters parameters)
    {
        if (parameters.MaximumChance < parameters.MinimumChance)
        {
            throw new InvalidOperationException("Maximum contest chance cannot be lower than minimum contest chance.");
        }
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        AttributeContestDebuffBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null)
        {
            return;
        }

        var chance = Math.Clamp(
            (context.Source.GetStat(parameters.SourceStat) - context.Target.GetStat(parameters.TargetStat)) * parameters.Scale,
            parameters.MinimumChance,
            parameters.MaximumChance);
        if (!Probability.RollChance(context.Random, chance))
        {
            return;
        }

        context.ApplyBuff(
            context.Target,
            parameters.BuffId,
            parameters.Level,
            parameters.Duration);
    }
}
