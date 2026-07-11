using Game.Core.Affix;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public static class BattleSkillTargeting
{
    private const string TaixuanCastSizeTalentId = "吴钩霜雪";
    private const string VajraWhipCastSizeTalentId = "金刚伏魔圈";
    private const string TaixuanSkillId = "太玄神功";
    private const string SunMoonWhipSkillId = "日月神鞭";
    private const int TaixuanCastSizeBonus = 3;
    private const int VajraWhipCastSize = 10;

    public static int ResolveEffectiveCastSize(BattleUnit source, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(skill);

        if (source.Character.HasEffectiveTalent(VajraWhipCastSizeTalentId) &&
            MatchesSourceSkill(skill, SunMoonWhipSkillId))
        {
            return VajraWhipCastSize;
        }

        var castSize = skill.CastSize;
        if (source.Character.HasEffectiveTalent(TaixuanCastSizeTalentId) &&
            MatchesSourceSkill(skill, TaixuanSkillId))
        {
            castSize += TaixuanCastSizeBonus;
        }

        return ApplyBlindPenalty(source, castSize);
    }

    public static int ResolveEffectiveImpactSize(BattleUnit source, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(skill);

        var impactSize = ApplyBlindPenalty(source, skill.ImpactSize);
        return source.HasTrait(TraitId.ExtendedSkillImpactSize)
            ? checked(impactSize + 1)
            : impactSize;
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
            .Where(targetUnit => ShouldIncludeTarget(state, source, targetUnit, skill))
            .ToList();
    }

    private static bool ShouldIncludeTarget(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        SkillInstance skill)
    {
        if (skill is LegendSkillInstance)
        {
            return state.AreEnemies(source, target);
        }

        var isOffensive = skill is ExternalSkillInstance or FormSkillInstance ||
                          skill is SpecialSkillInstance
                          {
                              Definition.Intent: SpecialSkillIntent.Offensive
                          };
        if (!isOffensive || state.AreEnemies(source, target))
        {
            return true;
        }

        return state.RuleSettings.Difficulty != GameDifficulty.Normal &&
               !source.HasTrait(TraitId.AvoidFriendlyFire);
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

    private static bool MatchesSourceSkill(SkillInstance skill, string sourceSkillId)
    {
        if (string.Equals(skill.Id, sourceSkillId, StringComparison.Ordinal))
        {
            return true;
        }

        return skill switch
        {
            FormSkillInstance formSkill => MatchesSourceSkill(formSkill.Parent, sourceSkillId),
            LegendSkillInstance legendSkill => MatchesSourceSkill(legendSkill.Parent, sourceSkillId),
            _ => false,
        };
    }
}
