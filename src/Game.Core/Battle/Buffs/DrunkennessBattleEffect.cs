using Game.Core.Affix;

namespace Game.Core.Battle.Buffs;

public sealed record DrunkennessBattleEffectParameters(
    double SkipChance,
    int Rage,
    string? SkipReason = null);

public sealed class DrunkennessBattleEffectHandler
    : CustomBattleEffectHandler<DrunkennessBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Validate(DrunkennessBattleEffectParameters parameters)
    {
        if (parameters.SkipChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Drunkenness skip chance must be between 0 and 1.");
        }

        if (parameters.Rage is < 0 or > BattleUnit.MaxRage)
        {
            throw new InvalidOperationException(
                $"Drunkenness rage must be between 0 and {BattleUnit.MaxRage}.");
        }
    }

    public override void Execute(
        IActionStartEffectContext context,
        DrunkennessBattleEffectParameters parameters)
    {
        if (Probability.RollChance(context.Random, parameters.SkipChance))
        {
            context.SkipCurrentAction(parameters.SkipReason);
            return;
        }

        context.SetRage(parameters.Rage, "drunkenness");
    }
}
