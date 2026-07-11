using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RageDamageBattleEffectParameters(
    [property: NonNegative] double BonusPerRage);

internal sealed class RageDamageBattleEffectHandler
    : CustomBattleEffectHandler<RageDamageBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        RageDamageBattleEffectParameters parameters)
    {
        if (context.Source is null ||
            !string.Equals(context.Unit.Id, context.Source.Id, StringComparison.Ordinal))
        {
            return;
        }

        TalentDamageModifier.MultiplyAttack(
            context,
            1d + context.Source.Rage * parameters.BonusPerRage);
    }
}
