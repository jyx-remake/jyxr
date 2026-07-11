using Game.Core.Model;

namespace Game.Core.Battle.SpecialSkills;

public sealed record FirearmDamageBattleEffectParameters;

public sealed class FirearmDamageBattleEffectHandler
    : CustomAbilityBattleEffectHandler<FirearmDamageBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        FirearmDamageBattleEffectParameters parameters)
    {
        foreach (var target in context.Targets)
        {
            var fortuneDifference = (int)Math.Abs(
                context.Source.GetStat(StatType.Fuyuan) - target.GetStat(StatType.Fuyuan));
            var randomDamage = fortuneDifference == 0
                ? 0
                : context.Random.Next(5 * fortuneDifference, 20 * fortuneDifference + 1);
            context.ApplyDirectDamage(target, 200 + randomDamage, context.Skill.Id);
        }
    }
}
