using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ResourceBasedRecoveryBattleEffectParameters(
    [property: NonNegative] double CurrentHpFactor = 0d,
    [property: NonNegative] double MaximumHpFactor = 0d,
    [property: NonNegative] double CurrentMpFactor = 0d,
    [property: NonNegative] double MaximumMpFactor = 0d,
    [property: NonNegative] double CurrentHpFactorPerUnitLevel = 0d,
    [property: NonNegative] double MaximumHpFactorPerUnitLevel = 0d,
    [property: NonNegative] double CurrentMpFactorPerUnitLevel = 0d,
    [property: NonNegative] double MaximumMpFactorPerUnitLevel = 0d);

internal sealed class ResourceBasedRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<ResourceBasedRecoveryBattleEffectParameters, IRecoveryEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeRecoveryResolved };

    public override void Execute(
        IRecoveryEffectContext context,
        ResourceBasedRecoveryBattleEffectParameters parameters)
    {
        var level = context.Unit.Character.Level;
        var extra = context.RecoveryKind switch
        {
            BattleRecoveryKind.Hp =>
                context.Unit.Hp * (parameters.CurrentHpFactor + parameters.CurrentHpFactorPerUnitLevel * level) +
                context.Unit.MaxHp * (parameters.MaximumHpFactor + parameters.MaximumHpFactorPerUnitLevel * level),
            BattleRecoveryKind.Mp =>
                context.Unit.Mp * (parameters.CurrentMpFactor + parameters.CurrentMpFactorPerUnitLevel * level) +
                context.Unit.MaxMp * (parameters.MaximumMpFactor + parameters.MaximumMpFactorPerUnitLevel * level),
            _ => throw new ArgumentOutOfRangeException(nameof(context.RecoveryKind), context.RecoveryKind, null),
        };
        context.RecoveryAmount = checked(context.RecoveryAmount + (int)Math.Ceiling(extra));
    }
}
