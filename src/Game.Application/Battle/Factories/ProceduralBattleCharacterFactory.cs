using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Application;

internal sealed class ProceduralBattleCharacterFactory
{
    private const string BasicInternalSkillId = "基本内功";
    private static readonly string[] RandomBaseTemplateIds =
    [
        "小混混", "小混混2", "小混混3", "小混混4", "无量剑弟子", "全真派入门弟子", "童姥使者",
        "明教徒", "峨眉弟子", "青城弟子", "全真派弟子", "天龙门弟子", "丐帮弟子", "五毒教弟子",
    ];
    private readonly GameSession _session;
    public ProceduralBattleCharacterFactory(GameSession session) => _session = session;
    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private SkillMaxLevelPolicy SkillMaxLevelPolicy => _session.SkillMaxLevelPolicy;
    private GameConfig Config => _session.Config;
    public CharacterInstance CreateRandomParticipantCharacter(
        BattleRandomParticipantDefinition participant,
        int index,
        EquipmentInstanceFactory tempFactory,
        GameDifficulty battleDifficulty)
    {
        var character = participant.Boss
            ? CreateRandomBossCharacter(participant, index, tempFactory)
            : CreateRandomSoldierCharacter(participant, index, tempFactory);

        ApplyDifficultyRandomTalents(character, battleDifficulty);
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

        // Legacy raises NPC skill levels by round and clamps levels globally; it does not assign per-instance max levels here.
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

    public CharacterInstance CreateArenaOpponentCharacter(
        int hardLevel,
        int index,
        EquipmentInstanceFactory tempFactory)
    {
        var candidates = ContentRepository.GetCharacters()
            .Where(character => character.ArenaEnabled && IsArenaHardLevelMatch(character.Level, hardLevel))
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException($"Arena hard level '{hardLevel}' has no arena-enabled character candidates.");
        }

        var definition = PickRandom(candidates);
        return CharacterMapper.CreateInitial(
            $"arena_{hardLevel}_{index}_{definition.Id}",
            definition,
            tempFactory);
    }

    public void ApplyNpcRoundPowerUp(CharacterInstance character)
    {
        var round = State.Adventure.Round;
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(Config.RoundEnemyHpAddRatio);
        ArgumentOutOfRangeException.ThrowIfNegative(Config.RoundEnemyMpAddRatio);
        ArgumentOutOfRangeException.ThrowIfLessThan(Config.RoundsPerNpcSkillLevelIncrease, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(Config.AbsoluteSkillMaxLevel, 1);

        var changed = false;
        if (round > 1)
        {
            changed |= ScaleNpcRoundResource(
                character,
                StatType.MaxHp,
                Config.RoundEnemyHpAddRatio,
                round);
            changed |= ScaleNpcRoundResource(
                character,
                StatType.MaxMp,
                Config.RoundEnemyMpAddRatio,
                round);
        }

        var skillLevelBonus = round / Config.RoundsPerNpcSkillLevelIncrease;
        if (skillLevelBonus > 0)
        {
            foreach (var skill in character.ExternalSkills)
            {
                changed |= AddNpcRoundSkillLevel(skill, skillLevelBonus);
            }

            foreach (var skill in character.InternalSkills)
            {
                changed |= AddNpcRoundSkillLevel(skill, skillLevelBonus);
            }
        }

        if (changed)
        {
            character.RebuildSnapshot();
        }
    }

    private static bool ScaleNpcRoundResource(
        CharacterInstance character,
        StatType statType,
        double ratio,
        int round)
    {
        var currentValue = character.GetBaseStat(statType);
        if (currentValue <= 0)
        {
            return false;
        }

        var multiplier = 1d + ratio * (round - 1);
        var scaledValue = checked((int)(currentValue * multiplier));
        if (scaledValue == currentValue)
        {
            return false;
        }

        character.BaseStats[statType] = scaledValue;
        return true;
    }

    private bool AddNpcRoundSkillLevel(SkillInstance skill, int bonus)
    {
        var targetLevel = Math.Min(
            checked(skill.Level + bonus),
            Config.AbsoluteSkillMaxLevel);
        if (targetLevel == skill.Level)
        {
            return false;
        }

        skill.Level = targetLevel;
        return true;
    }

    public void ApplyDifficultyRandomTalents(CharacterInstance character, GameDifficulty battleDifficulty)
    {
        switch (battleDifficulty)
        {
            case GameDifficulty.Hard:
                TryAddRandomTalent(character, Config.EnemyRandomTalentIds);
                break;
            case GameDifficulty.Crazy:
                TryAddRandomTalent(character, Config.EnemyRandomTalentCrazy1Ids);
                TryAddRandomTalent(character, Config.EnemyRandomTalentCrazy2Ids);
                TryAddRandomTalent(character, Config.EnemyRandomTalentCrazy3Ids);
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

    private static bool IsArenaHardLevelMatch(int level, int hardLevel) =>
        hardLevel switch
        {
            >= 1 and <= 4 => level > (hardLevel - 1) * 5 && level <= hardLevel * 5,
            5 => level >= 25 && level < 30,
            6 => level >= 30,
            _ => false,
        };

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

}
