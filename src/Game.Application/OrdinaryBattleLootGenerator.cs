using Game.Core;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;

namespace Game.Application;

public static class OrdinaryBattleLootGenerator
{
    public static IReadOnlyList<OrdinaryBattleRewardDrop> Generate(
        BattleState state,
        IContentRepository contentRepository,
        int round,
        int playerTeam,
        double dropChance)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(dropChance, 0d);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dropChance, 1d);

        var drops = new List<OrdinaryBattleRewardDrop>();
        foreach (var enemyUnit in state.Units.Where(unit => unit.Team != playerTeam))
        {
            if (!Probability.RollChance(dropChance))
            {
                continue;
            }

            var candidates = ResolveDropCandidates(contentRepository, enemyUnit.Character.Level);
            if (candidates.Count == 0)
            {
                continue;
            }

            var item = PickRandom(candidates);
            if (item is EquipmentDefinition equipment)
            {
                drops.Add(new OrdinaryBattleEquipmentRewardDrop(
                    equipment,
                    GenerateEquipmentRolls(equipment, contentRepository, round)));
                continue;
            }

            drops.Add(new OrdinaryBattleStackRewardDrop(item, 1));
        }

        return drops;
    }

    public static IReadOnlyList<GeneratedEquipmentAffixRoll> GenerateEquipmentRolls(
        EquipmentDefinition equipment,
        IContentRepository contentRepository,
        int round) =>
        EquipmentRandomAffixGenerator.GenerateRolls(equipment, contentRepository, round);

    private static IReadOnlyList<ItemDefinition> ResolveDropCandidates(
        IContentRepository contentRepository,
        int enemyLevel) =>
        contentRepository.GetItems()
            .Where(item => item.CanDrop && enemyLevel >= (item.Level - 1) * 5)
            .ToArray();

    private static T PickRandom<T>(IReadOnlyList<T> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Random selection candidates cannot be empty.");
        }

        return values[Random.Shared.Next(values.Count)];
    }
}
