using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Application;

public sealed class BattleService
{
    private const int GridWidth = 11;
    private const int GridHeight = 4;
    private const string BasicInternalSkillId = "基本内功";
    private static readonly string[] RandomBaseTemplateIds =
    [
        "小混混",
        "小混混2",
        "小混混3",
        "小混混4",
        "无量剑弟子",
        "全真派入门弟子",
        "童姥使者",
        "明教徒",
        "峨眉弟子",
        "青城弟子",
        "全真派弟子",
        "天龙门弟子",
        "丐帮弟子",
        "五毒教弟子",
    ];

    private static readonly string[] RandomTalentPoolIds =
    [
        "清心",
        "自我主义",
        "金钟罩",
        "阴谋家",
        "轻功大师",
        "飘然",
        "破甲",
        "至空至明",
        "好色",
        "大小姐",
    ];

    private static readonly string[] CrazyAttackTalentPoolIds =
    [
        "破甲",
        "铁拳无双",
        "嗜血狂魔",
    ];

    private static readonly string[] CrazyDefenceTalentPoolIds =
    [
        "金钟罩",
        "真气护体",
        "清心",
    ];

    private static readonly string[] CrazyOtherTalentPoolIds =
    [
        "轻功大师",
        "飘然",
        "至空至明",
    ];

    private readonly GameSession _session;

    public BattleService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private CharacterService CharacterService => _session.CharacterService;
    private int PlayerTeam => _session.Config.BattlePlayerTeam;

    public BattleState BuildBattleState(string battleId, IReadOnlyList<string> selectedCharacterIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        return BuildBattleState(ContentRepository.GetBattle(battleId), selectedCharacterIds);
    }

    public BattleState BuildBattleState(BattleDefinition battle, IReadOnlyList<string> selectedCharacterIds)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(selectedCharacterIds);

        var units = new List<BattleUnit>();
        var tempFactory = new EquipmentInstanceFactory();
        var slotCharacters = selectedCharacterIds
            .Select(ResolvePartyCharacter)
            .ToArray();

        foreach (var (participant, index) in battle.Participants.Select(static (participant, index) => (participant, index)))
        {
            var character = ResolveParticipantCharacter(participant, index, slotCharacters, tempFactory);
            if (character is null)
            {
                continue;
            }

            units.Add(CreateUnit(
                $"participant_{index}_{character.Id}",
                character,
                participant.Team,
                participant.Position,
                participant.Facing));
        }

        foreach (var (participant, index) in battle.RandomParticipants.Select(static (participant, index) => (participant, index)))
        {
            var character = CreateRandomParticipantCharacter(participant, index, tempFactory);
            units.Add(CreateUnit(
                $"random_{index}_{character.Id}",
                character,
                participant.Team,
                participant.Position,
                participant.Facing));
        }

        return new BattleState(new BattleGrid(GridWidth, GridHeight), units);
    }

    public OrdinaryBattleVictorySettlement PreviewOrdinaryVictorySettlement(
        BattleState state)
    {
        var rewardUnits = GetRewardEligiblePlayerUnits(state).ToArray();
        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            _session.Config.BattleGoldDropChance,
            PlayerTeam,
            rewardUnits.Length);
        var drops = OrdinaryBattleLootGenerator.Generate(
            state,
            ContentRepository,
            State.Adventure.Round,
            PlayerTeam,
            _session.Config.OrdinaryBattleDropChance);

        return settlement with { Drops = drops };
    }

    public void ApplyOrdinaryVictorySettlement(
        BattleState state,
        OrdinaryBattleVictorySettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settlement);

        foreach (var playerUnit in GetRewardEligiblePlayerUnits(state))
        {
            CharacterService.GainExperience(playerUnit.Character.Id, settlement.ExperiencePerMember);
        }

        if (settlement.Silver > 0 || settlement.Gold > 0)
        {
            State.Currency.AddSilver(settlement.Silver);
            State.Currency.AddGold(settlement.Gold);
            _session.Events.Publish(new CurrencyChangedEvent());
        }

        if (settlement.Drops.Count == 0)
        {
            return;
        }

        foreach (var drop in settlement.Drops)
        {
            ApplyRewardDrop(drop);
        }

        _session.Events.Publish(new InventoryChangedEvent());
    }

    private CharacterInstance CreateRandomParticipantCharacter(
        BattleRandomParticipantDefinition participant,
        int index,
        EquipmentInstanceFactory tempFactory)
    {
        var character = participant.Boss
            ? CreateRandomBossCharacter(participant, index, tempFactory)
            : CreateRandomSoldierCharacter(participant, index, tempFactory);

        ApplyDifficultyRandomTalents(character);
        character.RebuildSnapshot();
        return character;
    }

    private CharacterInstance CreateRandomSoldierCharacter(
        BattleRandomParticipantDefinition participant,
        int index,
        EquipmentInstanceFactory tempFactory)
    {
        var templateId = PickRandom(RandomBaseTemplateIds);
        var template = ContentRepository.GetCharacter(templateId);
        var character = CharacterMapper.CreateInitial(
            $"battle_random_{index}_{template.Id}",
            template,
            tempFactory);
        var resolvedLevel = ResolveRandomSoldierLevel(participant.Tier);
        var externalSkill = PickRandomSkillByTier(participant.Tier);

        character.Name = string.IsNullOrWhiteSpace(participant.Name) ? template.Name : participant.Name.Trim();
        if (!string.IsNullOrWhiteSpace(participant.Model))
        {
            character.Model = participant.Model.Trim();
        }

        character.SetLevel(resolvedLevel);
        character.BaseStats.Clear();
        var coreStat = ResolveRandomSoldierCoreStat(participant.Tier);
        SetBaseStat(character, StatType.Bili, coreStat);
        SetBaseStat(character, StatType.Dingli, coreStat);
        SetBaseStat(character, StatType.Fuyuan, coreStat);
        SetBaseStat(character, StatType.Gengu, coreStat);
        SetBaseStat(character, StatType.Shenfa, coreStat);
        SetBaseStat(character, StatType.Wuxing, coreStat);
        SetBaseStat(character, StatType.Quanzhang, coreStat);
        SetBaseStat(character, StatType.Jianfa, coreStat);
        SetBaseStat(character, StatType.Daofa, coreStat);
        SetBaseStat(character, StatType.Qimen, coreStat);
        SetBaseStat(character, StatType.MaxHp, resolvedLevel * 70);
        SetBaseStat(character, StatType.MaxMp, resolvedLevel * 70);

        foreach (var slot in character.EquippedItems.Keys.ToArray())
        {
            character.RemoveEquipment(slot);
        }

        character.UnlockedTalents.Clear();
        character.SpecialSkills.Clear();
        character.ExternalSkills.Clear();
        character.InternalSkills.Clear();
        character.EquipInternalSkill(null);

        character.ExternalSkills.Add(new ExternalSkillInstance(externalSkill, character, true)
        {
            Level = Random.Shared.Next(1, 7),
            Exp = 0,
        });

        var internalSkill = ContentRepository.GetInternalSkill(BasicInternalSkillId);
        character.InternalSkills.Add(new InternalSkillInstance(internalSkill, character)
        {
            Level = 10,
            Exp = 0,
        });
        character.EquipInternalSkill(BasicInternalSkillId);
        return character;
    }

    private CharacterInstance CreateRandomBossCharacter(
        BattleRandomParticipantDefinition participant,
        int index,
        EquipmentInstanceFactory tempFactory)
    {
        var candidates = ContentRepository.GetCharacters()
            .Where(character => character.ArenaEnabled && IsBossTierMatch(character.Level, participant.Tier))
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                $"Battle random boss tier '{participant.Tier}' has no arena-enabled character candidates.");
        }

        var definition = PickRandom(candidates);
        var character = CharacterMapper.CreateInitial(
            $"battle_random_boss_{index}_{definition.Id}",
            definition,
            tempFactory);
        if (!string.IsNullOrWhiteSpace(participant.Name))
        {
            character.Name = participant.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(participant.Model))
        {
            character.Model = participant.Model.Trim();
        }

        return character;
    }

    private CharacterInstance? ResolveParticipantCharacter(
        BattleParticipantDefinition participant,
        int index,
        IReadOnlyList<CharacterInstance?> slotCharacters,
        EquipmentInstanceFactory tempFactory)
    {
        if (!string.IsNullOrWhiteSpace(participant.CharacterId))
        {
            if (participant.Team == PlayerTeam &&
                State.Party.TryGetCharacter(participant.CharacterId, out var partyCharacter))
            {
                return partyCharacter;
            }

            var definition = ContentRepository.GetCharacter(participant.CharacterId);
            return CharacterMapper.CreateInitial(
                $"battle_{index}_{definition.Id}",
                definition,
                tempFactory);
        }

        if (participant.PartyIndex is not { } partyIndex ||
            partyIndex < 0 ||
            partyIndex >= slotCharacters.Count)
        {
            return null;
        }

        return slotCharacters[partyIndex];
    }

    private void ApplyDifficultyRandomTalents(CharacterInstance character)
    {
        switch (State.Adventure.Difficulty)
        {
            case GameDifficulty.Hard:
                TryAddRandomTalent(character, RandomTalentPoolIds);
                break;
            case GameDifficulty.Crazy:
                TryAddRandomTalent(character, CrazyAttackTalentPoolIds);
                TryAddRandomTalent(character, CrazyDefenceTalentPoolIds);
                TryAddRandomTalent(character, CrazyOtherTalentPoolIds);
                break;
        }
    }

    private void TryAddRandomTalent(CharacterInstance character, IReadOnlyList<string> candidateIds)
    {
        var availableIds = candidateIds
            .Where(candidateId => !character.HasTalent(candidateId))
            .Where(candidateId => IsTalentAllowedForGender(candidateId, character.Definition.Gender))
            .ToArray();
        if (availableIds.Length == 0)
        {
            return;
        }

        var talentId = PickRandom(availableIds);
        character.UnlockedTalents.Add(ContentRepository.GetTalent(talentId));
    }

    private static bool IsTalentAllowedForGender(string talentId, CharacterGender gender) =>
        talentId switch
        {
            "好色" => gender is not CharacterGender.Female,
            "大小姐" => gender is CharacterGender.Female,
            _ => true,
        };

    private ExternalSkillDefinition PickRandomSkillByTier(int tier)
    {
        var (minHard, maxHard) = ResolveRandomSkillHardRange(tier);
        var candidates = ContentRepository.GetExternalSkills()
            .Where(skill => skill.Hard >= minHard && skill.Hard <= maxHard)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                $"Random battle participant tier '{tier}' has no external skill candidates in hard range [{minHard}, {maxHard}].");
        }

        return PickRandom(candidates);
    }

    private static (double MinHard, double MaxHard) ResolveRandomSkillHardRange(int tier) =>
        tier switch
        {
            0 => (0d, 4d),
            1 => (5d, 6d),
            2 => (7d, 9d),
            3 => (10d, 100d),
            _ => throw new InvalidOperationException($"Unsupported non-boss random participant tier '{tier}'."),
        };

    private static int ResolveRandomSoldierLevel(int tier) =>
        tier switch
        {
            0 => Random.Shared.Next(1, 6),
            1 => Random.Shared.Next(6, 11),
            2 => Random.Shared.Next(11, 16),
            3 => Random.Shared.Next(16, 21),
            _ => throw new InvalidOperationException($"Unsupported non-boss random participant tier '{tier}'."),
        };

    private static int ResolveRandomSoldierCoreStat(int tier) =>
        tier switch
        {
            0 => 30,
            1 => 50,
            2 => 70,
            3 => 90,
            _ => throw new InvalidOperationException($"Unsupported non-boss random participant tier '{tier}'."),
        };

    private static bool IsBossTierMatch(int level, int tier)
    {
        if (tier < 5)
        {
            return tier * 5 < level && (tier + 1) * 5 >= level;
        }

        return level > tier * 5;
    }

    private static void SetBaseStat(CharacterInstance character, StatType statType, int value)
    {
        if (value <= 0)
        {
            return;
        }

        character.BaseStats[statType] = value;
    }

    private static T PickRandom<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot pick a random item from an empty list.");
        }

        return items[Random.Shared.Next(0, items.Count)];
    }

    private CharacterInstance? ResolvePartyCharacter(string characterId) =>
        State.Party.TryGetCharacter(characterId, out var character) ? character : null;

    private IEnumerable<BattleUnit> GetRewardEligiblePlayerUnits(BattleState state) =>
        state.Units.Where(unit =>
            unit.Team == PlayerTeam &&
            State.Party.ContainsMember(unit.Character.Id));

    private void ApplyRewardDrop(OrdinaryBattleRewardDrop drop)
    {
        switch (drop)
        {
            case OrdinaryBattleStackRewardDrop stack:
                State.Inventory.AddItem(stack.Item, stack.Quantity);
                return;

            case OrdinaryBattleEquipmentRewardDrop equipment:
                var extraAffixes = equipment.Rolls
                    .SelectMany(static roll => roll.Affixes)
                    .ToArray();
                var equipmentInstance = State.EquipmentInstanceFactory.Create(equipment.Equipment, extraAffixes);
                State.Inventory.AddEquipmentInstance(equipmentInstance);
                return;

            default:
                throw new InvalidOperationException($"Unsupported ordinary battle reward drop type '{drop.GetType().Name}'.");
        }
    }

    private static BattleUnit CreateUnit(
        string id,
        CharacterInstance character,
        int team,
        GridPosition position,
        int facing) =>
        new(
            id,
            character,
            team,
            position,
            facing <= 0 ? BattleFacing.Left : BattleFacing.Right);
}
