namespace Game.Core.Battle.SpecialSkills;

public sealed record XiangErWishDamageBattleEffectParameters;

public sealed class XiangErWishDamageBattleEffectHandler
    : CustomAbilityBattleEffectHandler<XiangErWishDamageBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        XiangErWishDamageBattleEffectParameters parameters)
    {
        var maximumDamage = 2000 + 100 * context.Source.Character.Level;
        foreach (var target in context.Targets)
        {
            var damage = context.Random.Next(1000, maximumDamage + 1);
            context.ApplyDirectDamage(target, damage, context.Skill.Id);
        }
    }
}
