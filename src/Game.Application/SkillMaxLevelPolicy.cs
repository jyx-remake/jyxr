using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Application;

public sealed class SkillMaxLevelPolicy
{
    private readonly Func<GameConfig> _configProvider;
    private readonly Func<GameProfile> _profileProvider;
    private readonly Func<int> _roundProvider;

    public SkillMaxLevelPolicy(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _configProvider = () => session.Config;
        _profileProvider = () => session.Profile;
        _roundProvider = () => session.State.Adventure.Round;
    }

    public SkillMaxLevelPolicy(GameConfig? config = null, GameProfile? profile = null, int round = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        var resolvedConfig = config ?? new GameConfig();
        var resolvedProfile = profile ?? new GameProfile();
        _configProvider = () => resolvedConfig;
        _profileProvider = () => resolvedProfile;
        _roundProvider = () => round;
    }

    public int GetMaxLevel(SkillInstance skill) =>
        skill switch
        {
            ExternalSkillInstance externalSkill => Math.Max(
                externalSkill.Level,
                GetExternalSkillMaxLevel(externalSkill.Definition.Id)),
            InternalSkillInstance internalSkill => Math.Max(
                internalSkill.Level,
                GetInternalSkillMaxLevel(internalSkill.Definition.Id)),
            FormSkillInstance formSkill => GetMaxLevel(formSkill.Parent),
            _ => throw new NotSupportedException($"Skill '{skill.GetType().Name}' does not have a mastery max level."),
        };

    public int GetMaxLevel(ExternalSkillDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return GetExternalSkillMaxLevel(definition.Id);
    }

    public int GetMaxLevel(InternalSkillDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return GetInternalSkillMaxLevel(definition.Id);
    }

    public int GetExternalSkillMaxLevel(string skillId) =>
        ResolveMaxLevel(_configProvider().BaseExternalSkillMaxLevel, skillId);

    public int GetInternalSkillMaxLevel(string skillId) =>
        ResolveMaxLevel(_configProvider().BaseInternalSkillMaxLevel, skillId);

    public int GetExternalSkillMaxLevelWithoutRoundBonus(string skillId) =>
        ResolveMaxLevelWithoutRoundBonus(_configProvider().BaseExternalSkillMaxLevel, skillId);

    public int GetInternalSkillMaxLevelWithoutRoundBonus(string skillId) =>
        ResolveMaxLevelWithoutRoundBonus(_configProvider().BaseInternalSkillMaxLevel, skillId);

    public int GetMaxLevelCommandRoundBonus()
    {
        var config = _configProvider();
        return CalculateRoundBonus(_roundProvider(), config.RoundsPerMaxLevelCommandIncrease);
    }

    private int ResolveMaxLevel(int baseMaxLevel, string skillId)
    {
        var config = _configProvider();
        var roundBonus = CalculateRoundBonus(_roundProvider(), config.RoundsPerMaxSkillLevelIncrease);
        return ResolveMaxLevelCore(config, baseMaxLevel, skillId, roundBonus);
    }

    private int ResolveMaxLevelWithoutRoundBonus(int baseMaxLevel, string skillId) =>
        ResolveMaxLevelCore(_configProvider(), baseMaxLevel, skillId, roundBonus: 0);

    private int ResolveMaxLevelCore(
        GameConfig config,
        int baseMaxLevel,
        string skillId,
        int roundBonus)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseMaxLevel, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        ArgumentOutOfRangeException.ThrowIfNegative(roundBonus);
        var absoluteMaxLevel = config.AbsoluteSkillMaxLevel;
        ArgumentOutOfRangeException.ThrowIfLessThan(absoluteMaxLevel, 1);
        return Math.Min(
            checked(baseMaxLevel + _profileProvider().GetSkillMaxLevelBonus(skillId) + roundBonus),
            absoluteMaxLevel);
    }

    private static int CalculateRoundBonus(int round, int roundsPerIncrease)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundsPerIncrease, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        return round / roundsPerIncrease;
    }
}
