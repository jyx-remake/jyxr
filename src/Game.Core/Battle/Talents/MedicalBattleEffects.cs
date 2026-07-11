using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record MedicalImmortalBattleEffectParameters;

internal sealed class MedicalImmortalBattleEffectHandler
    : CustomBattleEffectHandler<MedicalImmortalBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        MedicalImmortalBattleEffectParameters parameters)
    {
        foreach (var target in context.State.GetLivingUnits()
                     .Where(target => target.Team == context.Unit.Team)
                     .Where(target => target.Position.ManhattanDistanceTo(context.Unit.Position) <= 4))
        {
            var amount = RollBaseRecovery(context);
            context.ApplyHpRecovery(target, amount);
        }
    }

    internal static int RollBaseRecovery(IActionStartEffectContext context) =>
        (int)(context.Unit.GetStat(StatType.Gengu) * (1d + context.Random.NextDouble() * 3d));
}

public sealed record HealTheWoundedBattleEffectParameters;

internal sealed class HealTheWoundedBattleEffectHandler
    : CustomBattleEffectHandler<HealTheWoundedBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        HealTheWoundedBattleEffectParameters parameters)
    {
        var candidates = context.State.GetLivingUnits()
            .Where(target => target.Team == context.Unit.Team)
            .Where(target => !string.Equals(target.Id, context.Unit.Id, StringComparison.Ordinal))
            .Where(target => target.Position.ManhattanDistanceTo(context.Unit.Position) <= 4)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var target = candidates[context.Random.Next(0, candidates.Count)];
        var amount = 2 * MedicalImmortalBattleEffectHandler.RollBaseRecovery(context);
        context.ApplyHpRecovery(target, amount);
    }
}
