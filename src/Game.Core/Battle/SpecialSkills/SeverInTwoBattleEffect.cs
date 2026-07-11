namespace Game.Core.Battle.SpecialSkills;

public sealed record SeverInTwoBattleEffectParameters;

public sealed class SeverInTwoBattleEffectHandler
    : CustomAbilityBattleEffectHandler<SeverInTwoBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        SeverInTwoBattleEffectParameters parameters)
    {
        foreach (var target in context.Targets)
        {
            if (context.Random.NextDouble() < 0.5d)
            {
                context.ApplyDirectDamage(target, target.Hp / 2, context.Skill.Id);
            }
        }
    }
}
