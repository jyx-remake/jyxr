namespace Game.Core.Battle;

internal static class BattleTargetResolver
{
    public static IReadOnlyList<BattleUnit> Resolve(
        BattleState state,
        BattleUnit contextUnit,
        BattleUnit source,
        IReadOnlyList<BattleUnit> primaryTargets,
        BattleTargetSelectorDefinition selector) =>
        selector switch
        {
            SelfBattleTargetSelectorDefinition => [contextUnit],
            SourceBattleTargetSelectorDefinition => [source],
            TargetBattleTargetSelectorDefinition => primaryTargets,
            AllAlliesBattleTargetSelectorDefinition allAllies => state.GetLivingUnits()
                .Where(unit => unit.Team == contextUnit.Team)
                .Where(unit => allAllies.IncludeSelf || !string.Equals(unit.Id, contextUnit.Id, StringComparison.Ordinal))
                .ToList(),
            AllEnemiesBattleTargetSelectorDefinition => state.GetLivingUnits()
                .Where(unit => unit.Team != contextUnit.Team)
                .ToList(),
            NearbyAlliesBattleTargetSelectorDefinition nearbyAllies => state.GetLivingUnits()
                .Where(unit => unit.Team == contextUnit.Team)
                .Where(unit => nearbyAllies.IncludeSelf || !string.Equals(unit.Id, contextUnit.Id, StringComparison.Ordinal))
                .Where(unit => unit.Position.ManhattanDistanceTo(contextUnit.Position) <= nearbyAllies.Radius)
                .ToList(),
            _ => throw new NotSupportedException($"Unsupported battle target selector '{selector.GetType().Name}'."),
        };
}
