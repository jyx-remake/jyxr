using Game.Core.Model;

namespace Game.Core.Battle.SpecialSkills;

public sealed record ShoulderThrowBattleEffectParameters;

public sealed class ShoulderThrowBattleEffectHandler
    : CustomAbilityBattleEffectHandler<ShoulderThrowBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        ShoulderThrowBattleEffectParameters parameters)
    {
        foreach (var target in context.Targets)
        {
            var offsetX = context.Source.Position.X - target.Position.X;
            var offsetY = context.Source.Position.Y - target.Position.Y;
            var destination = new GridPosition(
                context.Source.Position.X + offsetX,
                context.Source.Position.Y + offsetY);
            context.TryRelocate(target, destination);

            var resolveDifference = (int)Math.Abs(
                context.Source.GetStat(StatType.Dingli) - target.GetStat(StatType.Dingli));
            var damage = resolveDifference == 0
                ? 0
                : context.Random.Next(resolveDifference * 5, resolveDifference * 20 + 1);
            context.ApplyDirectDamage(target, damage, context.Skill.Id);
        }
    }

    public override int? EstimateDamage(
        BattleAbilityDamageEstimateContext context,
        ShoulderThrowBattleEffectParameters parameters)
    {
        var resolveDifference = Math.Abs(
            context.Source.GetStat(StatType.Dingli) - context.Target.GetStat(StatType.Dingli));
        return (int)(12.5d * resolveDifference);
    }
}
