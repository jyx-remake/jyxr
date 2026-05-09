using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public static IReadOnlySet<GridPosition> GetImpactPositions(
        GridPosition source,
        GridPosition target,
        SkillImpactType impactType,
        int impactSize) =>
        ResolveImpactPositions(source, target, impactType, impactSize);

    private static IReadOnlySet<GridPosition> ResolveImpactPositions(
        GridPosition source,
        GridPosition target,
        SkillImpactType impactType,
        int impactSize)
    {
        var size = Math.Max(0, impactSize);
        return impactType switch
        {
            SkillImpactType.Single => new HashSet<GridPosition> { target },
            SkillImpactType.Plus => EnumerateSquare(target, size)
                .Where(position => position.X == target.X || position.Y == target.Y)
                .ToHashSet(),
            SkillImpactType.Star => EnumerateSquare(target, size)
                .Where(position =>
                    position.X == target.X ||
                    position.Y == target.Y ||
                    Math.Abs(position.X - target.X) == Math.Abs(position.Y - target.Y))
                .ToHashSet(),
            // TODO: Square should use radius semantics after skill data is migrated from legacy cover size.
            SkillImpactType.Square => EnumerateSquare(target, size / 2).ToHashSet(),
            SkillImpactType.Ring => EnumerateSquare(target, size)
                .Where(position => position.ManhattanDistanceTo(target) == size)
                .ToHashSet(),
            SkillImpactType.X => EnumerateSquare(target, size)
                .Where(position => position == target || Math.Abs(position.X - target.X) == Math.Abs(position.Y - target.Y))
                .ToHashSet(),
            SkillImpactType.Line => ResolveLinePositions(source, target, Math.Max(1, size)).ToHashSet(),
            SkillImpactType.Fan => ResolveFanPositions(source, target, Math.Max(1, size)).ToHashSet(),
            SkillImpactType.Cleave => ResolveCleavePositions(source, target).ToHashSet(),
            _ => new HashSet<GridPosition> { target },
        };
    }

    private static IEnumerable<GridPosition> EnumerateSquare(GridPosition center, int radius)
    {
        for (var y = center.Y - radius; y <= center.Y + radius; y++)
        {
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                yield return new GridPosition(x, y);
            }
        }
    }

    private static IEnumerable<GridPosition> ResolveLinePositions(GridPosition source, GridPosition target, int size)
    {
        var (dx, dy) = ResolvePrimaryDirection(source, target);
        for (var i = 1; i <= size; i++)
        {
            yield return new GridPosition(source.X + dx * i, source.Y + dy * i);
        }
    }

    private static IEnumerable<GridPosition> ResolveFanPositions(GridPosition source, GridPosition target, int size)
    {
        var (dx, dy) = ResolvePrimaryDirection(source, target);
        for (var distance = 1; distance <= size; distance++)
        {
            for (var offset = -distance + 1; offset <= distance - 1; offset++)
            {
                yield return dx != 0
                    ? new GridPosition(source.X + dx * distance, source.Y + offset)
                    : new GridPosition(source.X + offset, source.Y + dy * distance);
            }
        }
    }

    private static IEnumerable<GridPosition> ResolveCleavePositions(GridPosition source, GridPosition target)
    {
        yield return target;
        var (dx, dy) = ResolvePrimaryDirection(source, target);
        if (dx != 0)
        {
            yield return new GridPosition(target.X, target.Y - 1);
            yield return new GridPosition(target.X, target.Y + 1);
            yield break;
        }

        yield return new GridPosition(target.X - 1, target.Y);
        yield return new GridPosition(target.X + 1, target.Y);
    }

    private static (int Dx, int Dy) ResolvePrimaryDirection(GridPosition source, GridPosition target)
    {
        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return (dx < 0 ? -1 : 1, 0);
        }

        return (0, dy < 0 ? -1 : 1);
    }
}
