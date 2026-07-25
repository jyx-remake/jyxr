using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public interface IBattleStateFactory
{
    /// <summary>
    /// 构建战斗状态
    /// </summary>

    /// <summary>
    /// 创建战斗单位
    /// </summary>
    void CreateBattleCombatant(BattleState state, BattleJoinCombatant combatant);

    /// <summary>
    /// 生成战斗单位（召唤）
    /// </summary>
    IReadOnlyList<BattleJoinCombatant> SpawnCombatant(
        BattleUnit actingUnit,
        BattleState state,
        List<string> characterIds,
        IReadOnlyList<GridPosition> impactedPositions);
}
