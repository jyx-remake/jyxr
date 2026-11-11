using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Core.Battle;

public delegate IReadOnlyList<BattleJoinCombatant> SummonCombatantHandler(
    BattleUnit source,
    BattleState state,
    IReadOnlyList<string> characterIds,
    IReadOnlyList<GridPosition> impactedPositions);
