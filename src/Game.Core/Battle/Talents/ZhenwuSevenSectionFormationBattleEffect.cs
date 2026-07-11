using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ZhenwuFormationAttackBattleEffectParameters(
    string FormationTalentId,
    double MaxAttackIncrease = 0.1d,
    string? FloatText = null);

public sealed class ZhenwuFormationAttackBattleEffectHandler
    : CustomBattleEffectHandler<ZhenwuFormationAttackBattleEffectParameters>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(ZhenwuFormationAttackBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FormationTalentId);
        if (parameters.MaxAttackIncrease < 0d)
        {
            throw new InvalidOperationException("Formation max attack increase cannot be negative.");
        }
    }

    public override void Execute(
        BattleHookContext context,
        ZhenwuFormationAttackBattleEffectParameters parameters)
    {
        var calculation = context.DamageCalculation
            ?? throw new InvalidOperationException("Zhenwu attack formation requires a damage calculation context.");
        if (context.Skill?.Power is not > 0)
        {
            return;
        }

        foreach (var supporter in ZhenwuFormationMembers.GetOthers(context, parameters.FormationTalentId))
        {
            var increase = context.Random.NextDouble() * parameters.MaxAttackIncrease;
            calculation.AddModifier(
                BattleDamageContextField.FinalDamage,
                ModifierOp.More,
                1d + increase);

            if (!string.IsNullOrWhiteSpace(parameters.FloatText))
            {
                context.RequestFloatText(
                    supporter,
                    parameters.FloatText,
                    BattleFloatTextStyle.Negative);
            }
        }
    }
}

public sealed record ZhenwuFormationInterceptBattleEffectParameters(
    string FormationTalentId,
    double InterceptChance = 0.5d,
    double DamageFactor = 0.3d,
    string? Speech = null);

public sealed class ZhenwuFormationInterceptBattleEffectHandler
    : CustomBattleEffectHandler<ZhenwuFormationInterceptBattleEffectParameters>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Validate(ZhenwuFormationInterceptBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FormationTalentId);
        if (parameters.InterceptChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Formation intercept chance must be between 0 and 1.");
        }

        if (parameters.DamageFactor is < 0d or > 1d)
        {
            throw new InvalidOperationException("Formation damage factor must be between 0 and 1.");
        }
    }

    public override void Execute(
        BattleHookContext context,
        ZhenwuFormationInterceptBattleEffectParameters parameters)
    {
        if (context.DamageAmount is not > 0)
        {
            return;
        }

        foreach (var defender in ZhenwuFormationMembers.GetOthers(context, parameters.FormationTalentId))
        {
            if (!Probability.RollChance(context.Random, parameters.InterceptChance))
            {
                continue;
            }

            context.RedirectDamage(defender, parameters.DamageFactor);
            if (!string.IsNullOrWhiteSpace(parameters.Speech))
            {
                context.RequestSpeech(defender, parameters.Speech);
            }

            return;
        }
    }
}

internal static class ZhenwuFormationMembers
{
    public static IEnumerable<BattleUnit> GetOthers(
        BattleHookContext context,
        string formationTalentId) =>
        context.State.GetLivingUnits().Where(unit =>
            unit.Team == context.Unit.Team &&
            !string.Equals(
                unit.Character.Definition.Id,
                context.Unit.Character.Definition.Id,
                StringComparison.Ordinal) &&
            unit.Character.HasEffectiveTalent(formationTalentId));
}
