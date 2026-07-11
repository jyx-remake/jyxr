using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ResetSkillCooldownsBattleEffectParameters(
    [property: NotWhiteSpace] string TalentId);

internal sealed class ResetSkillCooldownsBattleEffectHandler
    : CustomBattleEffectHandler<ResetSkillCooldownsBattleEffectParameters, IHitResultEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeHitResolved };

    public override void Execute(
        IHitResultEffectContext context,
        ResetSkillCooldownsBattleEffectParameters parameters) =>
        context.ResetUnitSkillCooldowns(parameters.TalentId);
}
