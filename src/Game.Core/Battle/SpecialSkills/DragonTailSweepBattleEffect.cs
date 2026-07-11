using Game.Core.Model;

namespace Game.Core.Battle.SpecialSkills;

public sealed record DragonTailSweepBattleEffectParameters;

public sealed class DragonTailSweepBattleEffectHandler
    : CustomAbilityBattleEffectHandler<DragonTailSweepBattleEffectParameters>
{
    public override void Execute(
        IBattleAbilityEffectContext context,
        DragonTailSweepBattleEffectParameters parameters)
    {
        foreach (var target in context.Targets)
        {
            var directionX = target.Position.X - context.Source.Position.X;
            var directionY = target.Position.Y - context.Source.Position.Y;
            var destination = target.Position;
            var movedCells = 0;

            while (true)
            {
                var next = new GridPosition(destination.X + directionX, destination.Y + directionY);
                if (!context.IsCellAvailable(next, target))
                {
                    break;
                }

                destination = next;
                movedCells++;
            }

            if (movedCells > 0)
            {
                context.TryRelocate(target, destination);
            }

            var distanceFactor = movedCells + 1;
            var damage = context.Random.Next(distanceFactor * 100, distanceFactor * 500 + 1);
            context.ApplyDirectDamage(target, damage, context.Skill.Id);
        }
    }
}
