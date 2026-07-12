using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record VajraDemonSubduingFormationBattleEffectParameters(
    [property: NotWhiteSpace] string TalentId,
    [property: NonNegative] int RequiredMembers = 3,
    [property: NonNegative] double DamageFactor = 1.5d,
    BattleSpeechDefinition? Speech = null);

internal sealed class VajraDemonSubduingFormationBattleEffectHandler
    : CustomBattleEffectHandler<VajraDemonSubduingFormationBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        VajraDemonSubduingFormationBattleEffectParameters parameters)
    {
        if (context.Skill?.Power is not > 0)
        {
            return;
        }

        var members = context.State.GetLivingUnits()
            .Where(unit => unit.Team == context.Unit.Team)
            .Where(unit => unit.Character.HasEffectiveTalent(parameters.TalentId))
            .ToList();
        if (members.Count < parameters.RequiredMembers)
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.More,
            parameters.DamageFactor);

        var line = BattleSpeechRuntime.TryPickLine(parameters.Speech, context.Random);
        if (line is null)
        {
            return;
        }

        foreach (var member in members)
        {
            context.RequestSpeech(member, line);
        }
    }
}
