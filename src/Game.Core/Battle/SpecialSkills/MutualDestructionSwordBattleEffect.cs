namespace Game.Core.Battle.SpecialSkills;

public sealed record MutualDestructionSwordBattleEffectParameters;

public sealed class MutualDestructionSwordBattleEffectHandler
    : CustomAbilityBattleEffectHandler<MutualDestructionSwordBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        MutualDestructionSwordBattleEffectParameters parameters)
    {
        foreach (var target in context.Targets)
        {
            var damage = 1000 + (int)(target.MaxHp * context.Random.NextDouble() * 0.2d);
            context.ApplyDirectDamage(target, damage, context.Skill.Id);
        }

        context.ApplyDirectDamage(context.Source, 1000, context.Skill.Id);
    }
}
