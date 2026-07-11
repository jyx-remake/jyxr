using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record FormlessSwordQiBattleEffectParameters(
    [property: NotWhiteSpace] string SkillName,
    [property: NonNegative] int MpDifferenceCap,
    [property: NonNegative] double MinimumFactor,
    [property: NonNegative] double MaximumFactor);

internal sealed class FormlessSwordQiBattleEffectHandler
    : CustomBattleEffectHandler<FormlessSwordQiBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(FormlessSwordQiBattleEffectParameters parameters)
    {
        if (parameters.MaximumFactor < parameters.MinimumFactor)
        {
            throw new InvalidOperationException(
                "Formless sword Qi maximum factor cannot be lower than its minimum factor.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        FormlessSwordQiBattleEffectParameters parameters)
    {
        var source = context.Source;
        var target = context.Target;
        if (source is null ||
            target is null ||
            context.Skill is null ||
            !string.Equals(context.Unit.Id, source.Id, StringComparison.Ordinal) ||
            !context.State.AreEnemies(source, target) ||
            !string.Equals(context.Skill.Name, parameters.SkillName, StringComparison.Ordinal))
        {
            return;
        }

        var mpDifference = Math.Min(Math.Abs(source.Mp - target.Mp), parameters.MpDifferenceCap);
        var factor = parameters.MinimumFactor +
            (parameters.MaximumFactor - parameters.MinimumFactor) * context.Random.NextDouble();
        var additionalDamage = (int)Math.Floor(mpDifference * factor);
        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.PostAdd,
            additionalDamage);
    }
}
