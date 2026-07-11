using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record FlyingSkywardBattleEffectParameters;

internal sealed class FlyingSkywardBattleEffectHandler
    : CustomBattleEffectHandler<FlyingSkywardBattleEffectParameters, IHitConfirmedEffectContext>
{
    private const double TriggerChance = 0.3d;

    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Execute(
        IHitConfirmedEffectContext context,
        FlyingSkywardBattleEffectParameters parameters)
    {
        var source = context.Source;
        var target = context.Target;
        if (source is null ||
            target is null ||
            !target.IsAlive ||
            !context.State.AreEnemies(source, target) ||
            !Probability.RollChance(context.Random, TriggerChance))
        {
            return;
        }

        context.RequestSpeech(source, "给我飞吧！");

        var directionX = Math.Sign(target.Position.X - source.Position.X);
        var directionY = Math.Sign(target.Position.Y - source.Position.Y);
        if (directionX == 0 && directionY == 0)
        {
            return;
        }

        var destination = target.Position;
        while (true)
        {
            var next = new GridPosition(destination.X + directionX, destination.Y + directionY);
            if (!context.IsCellAvailable(next, target))
            {
                break;
            }

            destination = next;
        }

        if (destination != target.Position)
        {
            context.TryRelocate(target, destination);
        }
    }
}
