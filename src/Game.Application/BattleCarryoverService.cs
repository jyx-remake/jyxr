using Game.Core.Battle;

namespace Game.Application;

internal sealed class BattleCarryoverService(GameSession session)
{
    public void ApplyPlayerBattleCarryover(BattleState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        foreach (var playerUnit in GetPlayerPartyUnits(state))
        {
            playerUnit.Character.ApplyBattleCarryover(playerUnit.Hp, playerUnit.Mp, playerUnit.Rage);
        }
    }

    public void RestorePartyBattleResources()
    {
        foreach (var member in session.State.Party.Members)
        {
            member.RestoreBattleResources();
        }
    }

    private IEnumerable<BattleUnit> GetPlayerPartyUnits(BattleState state) =>
        state.Units.Where(unit =>
            unit.Team == session.Config.BattlePlayerTeam &&
            session.State.Party.ContainsMember(unit.Character.Id));
}
