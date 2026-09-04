using System.ComponentModel.DataAnnotations;
using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record CompanionDamageImmunityBattleEffectParameters(
    [property: Required, MinLength(1)] IReadOnlyList<string> CompanionNames);

internal sealed class CompanionDamageImmunityBattleEffectHandler
    : CustomBattleEffectHandler<CompanionDamageImmunityBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(CompanionDamageImmunityBattleEffectParameters parameters)
    {
        if (parameters.CompanionNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Companion names cannot contain blank values.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        CompanionDamageImmunityBattleEffectParameters parameters)
    {
        if (!context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Unit.Team &&
                parameters.CompanionNames.Contains(unit.Character.Definition.Name, StringComparer.Ordinal)))
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.More,
            0d);
    }
}
