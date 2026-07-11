using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record CarefulDefenseBattleEffectParameters(
    string Mode,
    string CarefulTalentId,
    string SmartTalentId,
    double CarefulChance,
    int CarefulDamageCap,
    double SmartChance,
    int SmartDamageCap,
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

        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.CarefulTalentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.SmartTalentId);
        ValidateChance(parameters.CarefulChance, nameof(parameters.CarefulChance));
        ValidateChance(parameters.SmartChance, nameof(parameters.SmartChance));
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.CarefulDamageCap);
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.SmartDamageCap);
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

    private static void ValidateChance(double chance, string parameterName)
    {
        if (chance is < 0d or > 1d)
        {
            throw new InvalidOperationException($"{parameterName} must be between 0 and 1.");
        }
    }
}

public sealed record ShiftingStarsReflectionBattleEffectParameters(
    string FamilyTalentId,
    double Chance,
    double FamilyChance,
    double DamageFactor,
    string FloatText,
    string Speech);

internal sealed class ShiftingStarsReflectionBattleEffectHandler
    : CustomBattleEffectHandler<ShiftingStarsReflectionBattleEffectParameters, IDamageApplicationRuntimeContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Validate(ShiftingStarsReflectionBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FamilyTalentId);
        ValidateProbability(parameters.Chance, nameof(parameters.Chance));
        ValidateProbability(parameters.FamilyChance, nameof(parameters.FamilyChance));
        ValidateProbability(parameters.DamageFactor, nameof(parameters.DamageFactor));
    }

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

    private static void ValidateProbability(double value, string parameterName)
    {
        if (value is < 0d or > 1d)
        {
            throw new InvalidOperationException($"{parameterName} must be between 0 and 1.");
        }
    }
}

public sealed record EternalSpringBattleEffectParameters(
    string EnhancedTalentId,
    double Chance,
    double EnhancedChance,
    double RecoveryFactor,
    string FloatText);

internal sealed class EternalSpringBattleEffectHandler
    : CustomBattleEffectHandler<EternalSpringBattleEffectParameters, IDamageApplicationRuntimeContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageApplied };

    public override void Validate(EternalSpringBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.EnhancedTalentId);
        ValidateProbability(parameters.Chance, nameof(parameters.Chance));
        ValidateProbability(parameters.EnhancedChance, nameof(parameters.EnhancedChance));
        ValidateProbability(parameters.RecoveryFactor, nameof(parameters.RecoveryFactor));
    }

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

    private static void ValidateProbability(double value, string parameterName)
    {
        if (value is < 0d or > 1d)
        {
            throw new InvalidOperationException($"{parameterName} must be between 0 and 1.");
        }
    }
}
