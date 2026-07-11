using Game.Core.Battle;
using Game.Core.Model;

namespace Game.Application;

internal sealed class BattleSettlementService(
    GameSession session,
    ZhenlongqijuBattleFactory zhenlongqijuFactory)
{
    private GameState State => session.State;
    private int PlayerTeam => session.Config.BattlePlayerTeam;

    public OrdinaryBattleVictorySettlement PreviewVictorySettlement(
        BattleState state,
        SpecialBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);
        var battle = session.ContentRepository.GetBattle(request.BattleId);
        return request is ZhenlongqijuBattleRequest zhenlongqiju
            ? PreviewZhenlongqijuSettlement(state, zhenlongqiju.Level)
            : PreviewOrdinarySettlement(state, battle.ExperienceMultiplier);
    }

    public void ApplyVictorySettlement(BattleState state, OrdinaryBattleVictorySettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settlement);

        foreach (var playerUnit in GetRewardUnits(state))
            session.CharacterService.GainExperience(playerUnit.Character.Id, settlement.ExperiencePerMember);

        if (settlement.Silver > 0)
        {
            State.Currency.AddSilver(settlement.Silver);
            session.Events.Publish(new CurrencyChangedEvent());
        }
        if (settlement.Gold > 0) session.ProfileService.AddYuanbao(settlement.Gold);

        var inventoryChanged = false;
        var profileChanged = false;
        foreach (var drop in settlement.Drops)
        {
            switch (drop)
            {
                case OrdinaryBattleStackRewardDrop stack:
                    State.Inventory.AddItem(stack.Item, stack.Quantity);
                    inventoryChanged = true;
                    break;
                case OrdinaryBattleEquipmentRewardDrop equipment:
                    var affixes = equipment.Rolls.SelectMany(static roll => roll.Affixes).ToArray();
                    State.Inventory.AddEquipmentInstance(
                        State.EquipmentInstanceFactory.Create(equipment.Equipment, affixes));
                    inventoryChanged = true;
                    break;
                case OrdinaryBattleSkillFragmentRewardDrop fragment:
                    session.ProfileService.AddSkillMaxLevelBonus(fragment.SkillId, fragment.Levels);
                    profileChanged = true;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported ordinary battle reward drop type '{drop.GetType().Name}'.");
            }
        }

        if (inventoryChanged) session.Events.Publish(new InventoryChangedEvent());
        if (profileChanged) session.Events.Publish(new ProfileChangedEvent());
    }

    private OrdinaryBattleVictorySettlement PreviewOrdinarySettlement(
        BattleState state,
        double experienceMultiplier)
    {
        var rewardUnitCount = GetRewardUnits(state).Count();
        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            session.Config.BattleGoldDropChance,
            PlayerTeam,
            rewardUnitCount,
            experienceMultiplier);
        var drops = OrdinaryBattleLootGenerator.Generate(
            state,
            session.ContentRepository,
            session.Config,
            session.SkillMaxLevelPolicy,
            State.Adventure.Difficulty,
            State.Adventure.Round,
            PlayerTeam,
            session.Config.OrdinaryBattleDropChance);
        return settlement with { Drops = drops };
    }

    private OrdinaryBattleVictorySettlement PreviewZhenlongqijuSettlement(BattleState state, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state, 0d, PlayerTeam, GetRewardUnits(state).Count());
        return settlement with
        {
            Gold = level / 2 + 1,
            Drops = zhenlongqijuFactory.GenerateDrops(level),
        };
    }

    private IEnumerable<BattleUnit> GetRewardUnits(BattleState state) =>
        state.Units.Where(unit =>
            unit.Team == PlayerTeam && State.Party.ContainsMember(unit.Character.Id));
}
