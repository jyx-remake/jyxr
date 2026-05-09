namespace Game.Core.Battle;

public interface IBattleAgent
{
    BattleTurnPlan Decide(BattleState state, string unitId);
}
