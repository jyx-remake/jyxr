using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record DefeatRecoveryBuffParameters(
    [property: Required] string BuffId,
    [property: NonNegative] int Level,
    [property: Positive] int Duration);

public sealed record DefeatRecoveryBattleEffectParameters(
    [property: Required] string AbilityId,
    [property: Probability] double MaximumHpFactor,
    [property: Positive] int UsageLimit,
    IReadOnlyList<DefeatRecoveryBuffParameters> Buffs,
    string? UsageId = null,
    string? FloatText = null,
    string? Speech = null);

internal sealed class DefeatRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<DefeatRecoveryBattleEffectParameters, IDefeatPreventionEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDefeated };

    public override void Validate(DefeatRecoveryBattleEffectParameters parameters)
    {
        if (parameters.Buffs is null)
        {
            throw new InvalidOperationException("Defeat recovery buffs cannot be null.");
        }

        if (parameters.Buffs.Any(buff => string.IsNullOrWhiteSpace(buff.BuffId)))
        {
            throw new InvalidOperationException("Defeat recovery buff IDs cannot be blank.");
        }
    }

    public override void Execute(
        IDefeatPreventionEffectContext context,
        DefeatRecoveryBattleEffectParameters parameters)
    {
        var usageId = string.IsNullOrWhiteSpace(parameters.UsageId)
            ? parameters.AbilityId
            : parameters.UsageId;
        if (context.Unit.GetAbilityUsageCount(usageId) >= parameters.UsageLimit)
        {
            return;
        }

        var recovery = Math.Max(1, (int)Math.Ceiling(context.Unit.MaxHp * parameters.MaximumHpFactor));
        context.ApplyHpRecovery(context.Unit, recovery, parameters.AbilityId);
        foreach (var buff in parameters.Buffs)
        {
            context.ApplyBuff(context.Unit, buff.BuffId, buff.Level, buff.Duration);
        }

        context.Unit.RecordAbilityUsage(usageId);
        SurviveAtOneHpBattleEffectHandler.Complete(
            context,
            parameters.AbilityId,
            parameters.FloatText,
            parameters.Speech);
    }
}
