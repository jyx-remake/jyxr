using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record FractionalResourceShiftBattleEffectParameters(
    [property: Probability] double Factor,
    string Resource,
    bool TransferToSource = true,
    int TargetActionGaugeDelta = 0,
    string? Detail = null);

internal sealed class FractionalResourceShiftBattleEffectHandler
    : CustomBattleEffectHandler<FractionalResourceShiftBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Validate(FractionalResourceShiftBattleEffectParameters parameters)
    {
        if (parameters.Resource is not ("rage" or "action_gauge"))
        {
            throw new InvalidOperationException(
                "Fractional resource shift resource must be 'rage' or 'action_gauge'.");
        }
    }

    public override void Execute(
        IHitConfirmedEffectContext context,
        FractionalResourceShiftBattleEffectParameters parameters)
    {
        if (context.Source is null || context.Target is null ||
            !context.State.AreEnemies(context.Source, context.Target))
        {
            return;
        }

        var amount = parameters.Resource switch
        {
            "rage" => (int)Math.Ceiling(context.Target.Rage * parameters.Factor),
            "action_gauge" => checked((int)Math.Ceiling(context.Target.ActionGauge * parameters.Factor)),
            _ => throw new InvalidOperationException(
                $"Unsupported fractional resource '{parameters.Resource}'."),
        };
        if (amount <= 0)
        {
            return;
        }

        if (parameters.Resource == "rage")
        {
            context.AddRage(context.Target, -amount, parameters.Detail);
            if (parameters.TransferToSource)
            {
                context.AddRage(context.Source, amount, parameters.Detail);
            }
        }
        else
        {
            context.AddActionGauge(context.Target, -amount);
            if (parameters.TransferToSource)
            {
                context.AddActionGauge(context.Source, amount);
            }
        }

        if (parameters.TargetActionGaugeDelta != 0)
        {
            context.AddActionGauge(context.Target, parameters.TargetActionGaugeDelta);
        }
    }
}
