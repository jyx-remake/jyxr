using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ResourceBasedRecoveryBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactor = 0d,
    [property: NonNegative] double MaximumHpFactor = 0d,
    [property: NonNegative] double CurrentMpFactor = 0d,
    [property: NonNegative] double MaximumMpFactor = 0d);

internal sealed class ResourceBasedRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<ResourceBasedRecoveryBattleEffectParameters, IRecoveryEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeRecoveryResolved };

    public override void Execute(
        IRecoveryEffectContext context,
        ResourceBasedRecoveryBattleEffectParameters parameters)
    {
        var extra = context.RecoveryKind switch
        {
            BattleRecoveryKind.Hp =>
                context.Unit.Hp * parameters.CurrentHpFactor +
                context.Unit.MaxHp * parameters.MaximumHpFactor,
            BattleRecoveryKind.Mp =>
                context.Unit.Mp * parameters.CurrentMpFactor +
                context.Unit.MaxMp * parameters.MaximumMpFactor,
            _ => throw new ArgumentOutOfRangeException(nameof(context.RecoveryKind), context.RecoveryKind, null),
        };
        context.RecoveryAmount = checked(context.RecoveryAmount + (int)Math.Ceiling(extra));
    }
}
