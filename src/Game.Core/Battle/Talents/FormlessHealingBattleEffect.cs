using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record FormlessHealingBattleEffectParameters(
    [property: NonNegative] int BaseValue,
    [property: NonNegative] int ValuePerLevel);

internal sealed class FormlessHealingBattleEffectHandler
    : CustomBattleEffectHandler<FormlessHealingBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        FormlessHealingBattleEffectParameters parameters)
    {
        var upperExclusive = checked(
            parameters.BaseValue + parameters.ValuePerLevel * context.Unit.Character.Level);
        var recovery = upperExclusive == 0
            ? 0
            : (int)(context.Random.NextDouble() * upperExclusive);
        var actual = context.ApplyHpRecovery(context.Unit, recovery, "无相");
        context.RequestFloatText(
            context.Unit,
            $"恢复生命{actual}",
            BattleFloatTextStyle.Recovery);
    }
}
