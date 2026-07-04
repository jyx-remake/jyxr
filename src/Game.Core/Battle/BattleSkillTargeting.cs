using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public static class BattleSkillTargeting
{
    public static int ResolveEffectiveCastSize(BattleUnit source, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(skill);

        return ApplyBlindPenalty(source, skill.CastSize);
    }

    public static int ResolveEffectiveImpactSize(BattleUnit source, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(skill);

        return ApplyBlindPenalty(source, skill.ImpactSize);
    }

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
            .Where(targetUnit =>
                !string.Equals(targetUnit.Id, source.Id, StringComparison.Ordinal) ||
                skill.CanTargetSelf)
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

    private static int ApplyBlindPenalty(BattleUnit source, int originalSize)
    {
        if (originalSize <= 0 ||
            source.HasTrait(TraitId.MindEye) ||
            source.TryGetBuff(BattleContentIds.Blind) is not { } blind)
        {
            return originalSize;
        }

        var penalty = (int)(blind.Level * 1.5d);
        return Math.Max(1, originalSize - penalty);
    }
}
