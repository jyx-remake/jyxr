using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record SurviveAtOneHpBattleEffectParameters(
    string AbilityId,
    double Chance,
    string? FloatText = null,
    string? Speech = null);

public sealed class SurviveAtOneHpBattleEffectHandler
    : CustomBattleEffectHandler<SurviveAtOneHpBattleEffectParameters, IDefeatPreventionEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDefeated };

    public override void Validate(SurviveAtOneHpBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.AbilityId);
        if (parameters.Chance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Survival chance must be between 0 and 1.");
        }
    }

    public override void Execute(
        IDefeatPreventionEffectContext context,
        SurviveAtOneHpBattleEffectParameters parameters)
    {
        if (!Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.Unit.RestoreHp(1);
        Complete(context, parameters.AbilityId, parameters.FloatText, parameters.Speech);
    }

    internal static void Complete(
        IDefeatPreventionEffectContext context,
        string abilityId,
        string? floatText,
        string? speech)
    {
        context.PreventDefeat(abilityId);
        if (!string.IsNullOrWhiteSpace(floatText))
        {
            context.RequestFloatText(context.Unit, floatText, BattleFloatTextStyle.Special);
        }

        if (!string.IsNullOrWhiteSpace(speech))
        {
            context.RequestSpeech(context.Unit, speech);
        }
    }
}

public sealed record QiShieldDefeatPreventionBattleEffectParameters(
    string AbilityId,
    double Chance,
    int MpCostPerDamage,
    string? FloatText = null,
    string? Speech = null);

public sealed class QiShieldDefeatPreventionBattleEffectHandler
    : CustomBattleEffectHandler<QiShieldDefeatPreventionBattleEffectParameters, IDefeatPreventionEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDefeated };

    public override void Validate(QiShieldDefeatPreventionBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.AbilityId);
        if (parameters.Chance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Qi shield chance must be between 0 and 1.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parameters.MpCostPerDamage);
    }

    public override void Execute(
        IDefeatPreventionEffectContext context,
        QiShieldDefeatPreventionBattleEffectParameters parameters)
    {
        var mpCost = checked(context.IncomingDamageAmount * parameters.MpCostPerDamage);
        if (context.Unit.Mp < mpCost || !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.Unit.RestoreHp(1);
        context.Unit.SpendMp(mpCost);
        SurviveAtOneHpBattleEffectHandler.Complete(
            context,
            parameters.AbilityId,
            parameters.FloatText,
            parameters.Speech);
    }
}

public sealed record EndlessFightingSpiritBattleEffectParameters(
    string AbilityId,
    string GuaranteedFirstTalentId,
    double Chance,
    string? FloatText = null,
    string? Speech = null);

public sealed class EndlessFightingSpiritBattleEffectHandler
    : CustomBattleEffectHandler<EndlessFightingSpiritBattleEffectParameters, IDefeatPreventionEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDefeated };

    public override void Validate(EndlessFightingSpiritBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.AbilityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.GuaranteedFirstTalentId);
        if (parameters.Chance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Revival chance must be between 0 and 1.");
        }
    }

    public override void Execute(
        IDefeatPreventionEffectContext context,
        EndlessFightingSpiritBattleEffectParameters parameters)
    {
        var guaranteed = context.Unit.GetAbilityUsageCount(parameters.AbilityId) == 0 &&
            context.Unit.Character.HasEffectiveTalent(parameters.GuaranteedFirstTalentId);
        if (!guaranteed && !Probability.RollChance(context.Random, parameters.Chance))
        {
            return;
        }

        context.Unit.RestoreHp(context.Unit.MaxHp);
        context.Unit.RestoreMp(context.Unit.MaxMp);
        context.Unit.SetRage(BattleUnit.MaxRage);
        context.Unit.RecordAbilityUsage(parameters.AbilityId);
        SurviveAtOneHpBattleEffectHandler.Complete(
            context,
            parameters.AbilityId,
            parameters.FloatText,
            parameters.Speech);
    }
}
