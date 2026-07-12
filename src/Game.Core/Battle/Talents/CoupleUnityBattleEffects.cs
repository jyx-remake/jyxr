using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record CoupleUnityBattleEffectParameters(
    [property: NotWhiteSpace] string TalentId,
    [property: NonNegative] double MinimumFactor = 1.2d,
    [property: NonNegative] double MaximumFactor = 2d,
    BattleSpeechDefinition? Speech = null);

internal sealed class CoupleUnityAttackBattleEffectHandler
    : CustomBattleEffectHandler<CoupleUnityBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(CoupleUnityBattleEffectParameters parameters) =>
        CoupleUnityBattleEffect.Validate(parameters);

    public override void Execute(
        IDamageCalculationEffectContext context,
        CoupleUnityBattleEffectParameters parameters)
    {
        var partner = CoupleUnityBattleEffect.FindLivingPartner(context, parameters.TalentId);
        if (context.Skill?.Power is not > 0 || partner is null)
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttackLow,
            ModifierOp.More,
            CoupleUnityBattleEffect.RollFactor(context, parameters));
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttackHigh,
            ModifierOp.More,
            CoupleUnityBattleEffect.RollFactor(context, parameters));
        CoupleUnityBattleEffect.TryRequestSpeech(context, partner, parameters.Speech);
    }
}

internal sealed class CoupleUnityDefenceBattleEffectHandler
    : CustomBattleEffectHandler<CoupleUnityBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(CoupleUnityBattleEffectParameters parameters) =>
        CoupleUnityBattleEffect.Validate(parameters);

    public override void Execute(
        IDamageCalculationEffectContext context,
        CoupleUnityBattleEffectParameters parameters)
    {
        var partner = CoupleUnityBattleEffect.FindLivingPartner(context, parameters.TalentId);
        if (context.Skill?.Power is not > 0 || partner is null)
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.TargetDefence,
            ModifierOp.More,
            CoupleUnityBattleEffect.RollFactor(context, parameters));
        CoupleUnityBattleEffect.TryRequestSpeech(context, partner, parameters.Speech);
    }
}

internal static class CoupleUnityBattleEffect
{
    public static void Validate(CoupleUnityBattleEffectParameters parameters)
    {
        if (parameters.MinimumFactor > parameters.MaximumFactor)
        {
            throw new InvalidOperationException(
                "Couple unity minimum factor cannot exceed its maximum factor.");
        }
    }

    public static BattleUnit? FindLivingPartner(IBattleEffectContext context, string talentId)
    {
        var partnerGender = context.Unit.Character.Definition.Gender switch
        {
            CharacterGender.Male => CharacterGender.Female,
            CharacterGender.Female => CharacterGender.Male,
            _ => (CharacterGender?)null,
        };
        if (partnerGender is null)
        {
            return null;
        }

        return context.State.GetLivingUnits().FirstOrDefault(unit =>
            unit.Team == context.Unit.Team &&
            unit.Id != context.Unit.Id &&
            unit.Character.Definition.Gender == partnerGender &&
            unit.Character.HasEffectiveTalent(talentId));
    }

    public static double RollFactor(
        IBattleEffectContext context,
        CoupleUnityBattleEffectParameters parameters) =>
        parameters.MinimumFactor +
        context.Random.NextDouble() * (parameters.MaximumFactor - parameters.MinimumFactor);

    public static void TryRequestSpeech(
        IBattleEffectContext context,
        BattleUnit partner,
        BattleSpeechDefinition? speech)
    {
        var line = BattleSpeechRuntime.TryPickLine(speech, context.Random);
        if (line is null)
        {
            return;
        }

        context.RequestSpeech(context.Unit, line);
        context.RequestSpeech(partner, line);
    }
}
