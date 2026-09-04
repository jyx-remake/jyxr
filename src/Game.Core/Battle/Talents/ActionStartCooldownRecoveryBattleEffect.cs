using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ActionStartCooldownRecoveryBattleEffectParameters(
    [property: Probability] double SkillChance,
    [property: Probability] double ItemChance);

internal sealed class ActionStartCooldownRecoveryBattleEffectHandler
    : CustomBattleEffectHandler<ActionStartCooldownRecoveryBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        ActionStartCooldownRecoveryBattleEffectParameters parameters)
    {
        if (Probability.RollChance(context.Random, parameters.SkillChance))
        {
            context.Unit.Character.RecoverSkillCooldowns();
        }

        if (context.Unit.ItemCooldown > 0 &&
            Probability.RollChance(context.Random, parameters.ItemChance))
        {
            context.Unit.RecoverItemCooldown();
        }
    }
}
