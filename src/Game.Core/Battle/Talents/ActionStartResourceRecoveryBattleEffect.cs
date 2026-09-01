using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ActionStartResourceRecoveryBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactor = 0d,
    [property: NonNegative] double MaximumHpFactor = 0d,
    [property: NonNegative] double CurrentMpFactor = 0d,
    [property: NonNegative] double MaximumMpFactor = 0d,
    string? Detail = null);

internal sealed class ActionStartResourceRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<ActionStartResourceRecoveryBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        ActionStartResourceRecoveryBattleEffectParameters parameters)
    {
        var hp = (int)Math.Ceiling(
            context.Unit.Hp * parameters.CurrentHpFactor +
            context.Unit.MaxHp * parameters.MaximumHpFactor);
        var mp = (int)Math.Ceiling(
            context.Unit.Mp * parameters.CurrentMpFactor +
            context.Unit.MaxMp * parameters.MaximumMpFactor);
        if (hp > 0)
        {
            context.ApplyHpRecovery(context.Unit, hp, parameters.Detail);
        }

        if (mp > 0)
        {
            context.ApplyMpRecovery(context.Unit, mp, parameters.Detail);
        }
    }
}
