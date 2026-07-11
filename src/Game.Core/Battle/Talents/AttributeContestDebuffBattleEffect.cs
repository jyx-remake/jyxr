using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record AttributeContestDebuffBattleEffectParameters(
    StatType SourceStat,
    StatType TargetStat,
    double Scale,
    string BuffId,
    int Level,
    int Duration,
    double MinimumChance = 0d,
    double MaximumChance = 1d);

internal sealed class AttributeContestDebuffBattleEffectHandler
    : CustomBattleEffectHandler<AttributeContestDebuffBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(AttributeContestDebuffBattleEffectParameters parameters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.Scale);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.BuffId);
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.Level);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parameters.Duration);
        if (parameters.MinimumChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Minimum contest chance must be between 0 and 1.");
        }

        if (parameters.MaximumChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Maximum contest chance must be between 0 and 1.");
        }

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
