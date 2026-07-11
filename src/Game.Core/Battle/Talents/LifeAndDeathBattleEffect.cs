using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record LifeAndDeathBattleEffectParameters;

internal sealed class LifeAndDeathBattleEffectHandler
    : CustomBattleEffectHandler<LifeAndDeathBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Execute(
        IDamageCalculationEffectContext context,
        LifeAndDeathBattleEffectParameters parameters)
    {
        var source = context.Source;
        var target = context.Target;
        if (source is null ||
            target is null ||
            !string.Equals(context.Unit.Id, source.Id, StringComparison.Ordinal) ||
            !context.State.AreEnemies(source, target) ||
            !Probability.RollChance(context.Random, 0.1d))
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.FinalDamage,
            ModifierOp.PostAdd,
            target.MaxHp * 0.1d);
        context.DamageCalculation.AdditionalLifestealRate += 0.5d;

        context.RequestSpeech(target, "啊！好疼！！");
        context.RequestSpeech(source, $"嘿嘿...（{source.Character.Name}天赋【死生茫茫】发动！）");
    }
}
