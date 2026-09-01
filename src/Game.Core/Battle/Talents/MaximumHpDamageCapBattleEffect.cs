using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record MaximumHpDamageCapBattleEffectParameters(
    [property: Probability] double MaximumHpFactor,
    string? FloatText = null);

internal sealed class MaximumHpDamageCapBattleEffectHandler
    : CustomBattleEffectHandler<MaximumHpDamageCapBattleEffectParameters, IDamageApplicationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Execute(
        IDamageApplicationEffectContext context,
        MaximumHpDamageCapBattleEffectParameters parameters)
    {
        var maximum = (int)Math.Floor(context.Unit.MaxHp * parameters.MaximumHpFactor);
        if (context.DamageAmount <= maximum)
        {
            return;
        }

        context.CapDamage(maximum);
        if (!string.IsNullOrWhiteSpace(parameters.FloatText))
        {
            context.RequestFloatText(
                context.Unit,
                parameters.FloatText,
                BattleFloatTextStyle.Special);
        }
    }
}
