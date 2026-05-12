using Game.Core;
using Game.Core.Battle;
using Game.Core.Model.Character;

namespace Game.Application;

public static class OrdinaryBattleVictorySettlementCalculator
{
    private const int DefaultPlayerTeam = 1;
    private const int MinimumExperiencePerMember = 5;
    private const int MinimumSilverReward = 10;

    public static OrdinaryBattleVictorySettlement Calculate(
        BattleState state,
        double goldDropChance,
        int playerTeam = DefaultPlayerTeam,
        int? rewardMemberCount = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(goldDropChance, 0d);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(goldDropChance, 1d);
        if (rewardMemberCount is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(rewardMemberCount.Value);
        }

        var playerUnitCount = rewardMemberCount ?? state.Units.Count(unit => unit.Team == playerTeam);
        if (playerUnitCount <= 0)
        {
            if (rewardMemberCount is null)
            {
                throw new InvalidOperationException("Ordinary battle settlement requires at least one player-team unit.");
            }
        }

        var enemyUnits = state.Units
            .Where(unit => unit.Team != playerTeam)
            .ToArray();

        var totalExperienceBudget = enemyUnits.Sum(unit =>
            CharacterLevelProgression.GetLevelUpExperience(unit.Character.Level) / 15d);
        var experiencePerMember = playerUnitCount > 0
            ? Math.Max(
                MinimumExperiencePerMember,
                (int)(totalExperienceBudget / playerUnitCount))
            : 0;

        var silver = Math.Max(
            MinimumSilverReward,
            enemyUnits.Sum(unit => (int)Math.Pow(1.2d, unit.Character.Level)));

        var gold = Probability.RollChance(goldDropChance) ? 1 : 0;

        return new OrdinaryBattleVictorySettlement(experiencePerMember, silver, gold);
    }
}
