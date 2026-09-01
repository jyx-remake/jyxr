using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record DamageDealtRecoveryBattleEffectParameters(
    [property: NonNegative] double HpFactor = 0d,
    [property: NonNegative] double MpFactor = 0d,
    string? Detail = null);

internal sealed class DamageDealtRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<DamageDealtRecoveryBattleEffectParameters, IDamageDealtEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnDamageDealt };

    public override void Validate(DamageDealtRecoveryBattleEffectParameters parameters)
    {
        if (parameters.HpFactor == 0d && parameters.MpFactor == 0d)
        {
            throw new InvalidOperationException("Damage-dealt recovery requires a positive HP or MP factor.");
        }
    }

    public override void Execute(
        IDamageDealtEffectContext context,
        DamageDealtRecoveryBattleEffectParameters parameters)
    {
        if (context.ActualDamageAmount <= 0 || context.Source is null || !context.Source.IsAlive)
        {
            return;
        }

        var hp = (int)Math.Floor(context.ActualDamageAmount * parameters.HpFactor);
        var mp = (int)Math.Floor(context.ActualDamageAmount * parameters.MpFactor);
        if (hp > 0)
        {
            context.ApplyHpRecovery(context.Source, hp, parameters.Detail);
        }

        if (mp > 0)
        {
            context.ApplyMpRecovery(context.Source, mp, parameters.Detail);
        }
    }
}
