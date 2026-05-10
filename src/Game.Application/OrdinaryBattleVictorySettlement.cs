namespace Game.Application;

public sealed record OrdinaryBattleVictorySettlement(
    int ExperiencePerMember,
    int Silver,
    int Gold,
    IReadOnlyList<OrdinaryBattleRewardDrop> Drops)
{
    public OrdinaryBattleVictorySettlement(
        int experiencePerMember,
        int silver,
        int gold)
        : this(experiencePerMember, silver, gold, [])
    {
    }
}
