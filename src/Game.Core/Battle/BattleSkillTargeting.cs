using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public static class BattleSkillTargeting
{
    public static IReadOnlyList<BattleUnit> ResolveEffectiveTargets(
        BattleState state,
        BattleUnit source,
        SkillInstance skill,
        IReadOnlySet<GridPosition> impactedPositions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(impactedPositions);

        return state.Units
            .Where(targetUnit => targetUnit.IsAlive && impactedPositions.Contains(targetUnit.Position))
            .Where(targetUnit => !string.Equals(targetUnit.Id, source.Id, StringComparison.Ordinal))
            .Where(targetUnit => skill is not LegendSkillInstance || state.AreEnemies(source, targetUnit))
            .ToList();
    }

    public static IReadOnlySet<GridPosition> EnumerateCastTargets(
        GridPosition sourcePosition,
        int castSize,
        BattleGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var targets = new HashSet<GridPosition>();
        for (var y = sourcePosition.Y - castSize; y <= sourcePosition.Y + castSize; y++)
        {
            for (var x = sourcePosition.X - castSize; x <= sourcePosition.X + castSize; x++)
            {
                var position = new GridPosition(x, y);
                if (grid.Contains(position) && sourcePosition.ManhattanDistanceTo(position) <= castSize)
                {
                    targets.Add(position);
                }
            }
        }

        return targets;
    }
}
