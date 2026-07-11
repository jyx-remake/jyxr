using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record CarefulDefenseBattleEffectParameters(
    string Mode,
    [property: NotWhiteSpace] string CarefulTalentId,
    [property: NotWhiteSpace] string SmartTalentId,
    [property: Probability] double CarefulChance,
    [property: NonNegative] int CarefulDamageCap,
    [property: Probability] double SmartChance,
    [property: NonNegative] int SmartDamageCap,
    string CarefulSpeech,
    string SmartSpeech);

internal sealed class CarefulDefenseBattleEffectHandler
    : CustomBattleEffectHandler<CarefulDefenseBattleEffectParameters, IDamageApplicationRuntimeContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Validate(CarefulDefenseBattleEffectParameters parameters)
    {
        if (parameters.Mode is not ("careful" or "smart"))
        {
            throw new InvalidOperationException("Careful defense mode must be 'careful' or 'smart'.");
        }

    }

    public override void Execute(
        IDamageApplicationRuntimeContext context,
        CarefulDefenseBattleEffectParameters parameters)
    {
        if (context.DamageAmount <= 0)
        {
            return;
        }

        var hasCareful = context.Unit.Character.HasEffectiveTalent(parameters.CarefulTalentId);
        var hasSmart = context.Unit.Character.HasEffectiveTalent(parameters.SmartTalentId);

        if (parameters.Mode == "careful")
        {
            if (hasSmart || !Probability.RollChance(context.Random, parameters.CarefulChance))
            {
                return;
            }

            ApplyCareful(context, parameters);
            return;
        }

        if (hasCareful && Probability.RollChance(context.Random, parameters.CarefulChance))
        {
            ApplyCareful(context, parameters);
            return;
        }

        if (!Probability.RollChance(context.Random, parameters.SmartChance))
        {
            return;
        }

        var overflow = Math.Max(0, context.DamageAmount - parameters.SmartDamageCap);
        context.CapDamage(parameters.SmartDamageCap);
        if (overflow > 0)
        {
            context.ApplyMpDamage(context.Unit, overflow, parameters.SmartTalentId);
        }

        context.RequestSpeech(context.Unit, parameters.SmartSpeech);
    }

    private static void ApplyCareful(
        IDamageApplicationRuntimeContext context,
        CarefulDefenseBattleEffectParameters parameters)
    {
        context.CapDamage(parameters.CarefulDamageCap);
        context.RequestSpeech(context.Unit, parameters.CarefulSpeech);
    }

}

public sealed record ShiftingStarsReflectionBattleEffectParameters(
    [property: NotWhiteSpace] string FamilyTalentId,
    [property: Probability] double Chance,
    [property: Probability] double FamilyChance,
    [property: Probability] double DamageFactor,
    string FloatText,
    string Speech);

internal sealed class ShiftingStarsReflectionBattleEffectHandler
    : CustomBattleEffectHandler<ShiftingStarsReflectionBattleEffectParameters, IDamageApplicationRuntimeContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Execute(
        IDamageApplicationRuntimeContext context,
        ShiftingStarsReflectionBattleEffectParameters parameters)
    {
        var source = context.Source;
        if (source is null || context.DamageAmount <= 0 ||
            !context.State.AreEnemies(context.Unit, source))
        {
            return;
        }

        var chance = context.Unit.Character.HasEffectiveTalent(parameters.FamilyTalentId)
            ? parameters.FamilyChance
            : parameters.Chance;
        if (!Probability.RollChance(context.Random, chance))
        {
            return;
        }

        var reflectedDamage = (int)(context.DamageAmount * parameters.DamageFactor);
        if (reflectedDamage > 0)
        {
            context.ApplyDirectDamage(source, reflectedDamage, "斗转星移");
        }

        context.RequestFloatText(context.Unit, parameters.FloatText, BattleFloatTextStyle.Special);
        context.RequestSpeech(context.Unit, parameters.Speech);
        context.CancelDamage(suppressHitEffects: true);
    }

}

public sealed record EternalSpringBattleEffectParameters(
    [property: NotWhiteSpace] string EnhancedTalentId,
    [property: Probability] double Chance,
    [property: Probability] double EnhancedChance,
    [property: Probability] double RecoveryFactor,
    string FloatText);

internal sealed class EternalSpringBattleEffectHandler
    : CustomBattleEffectHandler<EternalSpringBattleEffectParameters, IDamageApplicationRuntimeContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Execute(
        IDamageApplicationRuntimeContext context,
        EternalSpringBattleEffectParameters parameters)
    {
        if (context.DamageAmount <= 0)
        {
            return;
        }

        var chance = context.Unit.Character.HasEffectiveTalent(parameters.EnhancedTalentId)
            ? parameters.EnhancedChance
            : parameters.Chance;
        if (!Probability.RollChance(context.Random, chance))
        {
            return;
        }

        var recovery = (int)(context.DamageAmount * parameters.RecoveryFactor);
        var actual = context.ApplyHpRecovery(context.Unit, recovery, "不老长春");
        context.RequestFloatText(
            context.Unit,
            $"{parameters.FloatText}{actual}",
            BattleFloatTextStyle.Recovery);
        context.CancelDamage(suppressHitEffects: true);
    }

}
