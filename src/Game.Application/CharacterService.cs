using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Application;

public sealed class CharacterService
{
    private readonly GameSession _session;

    public CharacterService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private GameConfig Config => _session.Config;
    private SkillMaxLevelPolicy SkillMaxLevelPolicy => _session.SkillMaxLevelPolicy;
    private CharacterResourceLimitPolicy CharacterResourceLimitPolicy => _session.CharacterResourceLimitPolicy;

    public void RenameCharacter(string characterId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var character = GetPartyMember(characterId);
        character.Name = name;
        PublishCharacterChanged(character);
    }

    public void SetCharacterPortrait(string characterId, string portrait)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portrait);
        var character = GetPartyMember(characterId);
        character.Portrait = portrait;
        PublishCharacterChanged(character);
    }

    public void SetCharacterModel(string characterId, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        var character = GetPartyMember(characterId);
        character.Model = model;
        PublishCharacterChanged(character);
    }

    public void SetCharacterGender(string characterId, CharacterGender gender)
    {
        var character = GetPartyMember(characterId);
        character.SetGender(gender);
        PublishCharacterChanged(character);
    }

    public void SetGrowTemplate(string characterId, string growTemplateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(growTemplateId);
        var character = GetPartyMember(characterId);
        character.GrowTemplateId = growTemplateId;
        PublishCharacterChanged(character);
    }

    public void SetBattleAiType(string characterId, BattleAiType aiType)
    {
        var character = GetPartyMember(characterId);
        if (character.AiType == aiType)
        {
            return;
        }

        character.SetAiType(aiType);
        PublishCharacterChanged(character);
    }

    public void SetPersonality(string characterId, int personality, int? secondaryPersonality = null)
    {
        var character = GetPartyMember(characterId);
        character.SetPersonality(personality, secondaryPersonality);
        PublishCharacterChanged(character);
    }

    public void ReplaceBaseStats(string characterId, IReadOnlyDictionary<StatType, int> stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var character = GetPartyMember(characterId);
        character.BaseStats.Clear();
        foreach (var (statType, value) in stats)
        {
            if (value < 0)
            {
                throw new InvalidOperationException($"Base stat '{statType}' cannot be less than zero.");
            }

            if (value > 0)
            {
                character.BaseStats[statType] = value;
            }
        }

        // character.RebuildSnapshot();
        PublishCharacterChanged(character);
    }

    public void AddBaseStat(string characterId, string statName, int value)
    {
        var character = GetPartyMember(characterId);
        var statType = StatCatalog.Parse(statName);
        var previousValue = character.GetBaseStat(statType);
        if (CharacterResourceLimitPolicy.IsBaseResourceStat(statType))
        {
            // Legacy UPGRADE.MAXHP/MAXMP: the resulting maximum is clamped
            // into [100, round-scaled cap] and the current resource follows
            // the new cap (an injury story drains current HP/MP along with
            // it; a growth story refills it).
            var target = Math.Clamp(
                checked(previousValue + value),
                CharacterResourceLimitPolicy.MinHpMp,
                CharacterResourceLimitPolicy.GetMaxHpMp());
            value = checked(target - previousValue);
        }

        character.AddBaseStat(statType, value);
        if (CharacterResourceLimitPolicy.IsBaseResourceStat(statType))
        {
            CharacterResourceLimitPolicy.ClampBaseResourceStat(character, statType);
            character.SetCurrentResourceToCap(statType);
            character.ClampBattleResources();
        }

        var appliedDelta = character.GetBaseStat(statType) - previousValue;
        PublishToastAndCharacterChanged(character, $"{character.Name} {statName} {appliedDelta:+0;-0;0}");
    }

    public void ScaleStats(string characterId, double ratio)
    {
        if (ratio is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ratio), "Stat scale must be between 0 and 1.");
        }

        var character = GetPartyMember(characterId);

        foreach (var statType in StatCatalog.MinusMaxPointsStats)
        {
            var currentValue = character.GetBaseStat(statType);
            var targetValue = (int)(currentValue * ratio);
            var delta = targetValue - currentValue;
            if (delta != 0)
            {
                character.AddBaseStat(statType, delta);
            }
        }
        CharacterResourceLimitPolicy.ClampBaseResourceStats(character);
        character.ClampBattleResources();

        var currentPoints = character.UnspentStatPoints;
        var targetPoints = (int)(currentPoints * ratio);
        if (targetPoints != currentPoints)
        {
            character.SetUnspentStatPoints(targetPoints);
        }
        PublishToastAndCharacterChanged(character, $"{character.Name} 基础属性调整为 {ratio:P0}");
    }

    public void AllocateStat(string characterId, StatType statType, int points = 1)
    {
        var character = GetPartyMember(characterId);
        character.AllocateStat(statType, points);
        PublishCharacterChanged(character);
    }

    public void GrantStatPoints(string characterId, int points)
    {
        var character = GetPartyMember(characterId);
        character.GrantStatPoints(points);
        PublishToastAndCharacterChanged(character, $"{character.Name} 自由属性点 +{points}");
    }

    public void GainExperience(string characterId, int experience)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(experience);
        if (experience == 0)
        {
            return;
        }

        var character = GetPartyMember(characterId);
        var change = CharacterExperienceProgression.TryAddExperience(
            character,
            experience,
            Config.MaxLevel,
            () => ResolveGrowTemplate(character));
        if (change.LeveledUp)
        {
            CharacterResourceLimitPolicy.ClampBaseResourceStats(character);
            character.ClampBattleResources();

            _session.Events.Publish(new CharacterLeveledUpEvent(character.Id, change.OldLevel, change.NewLevel));
        }

        PublishCharacterChanged(character);
    }

    public void LevelUp(string characterId, int levels = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);

        var character = GetPartyMember(characterId);
        if (character.Level >= Config.MaxLevel)
        {
            return;
        }

        var targetLevel = Math.Min(character.Level + levels, Config.MaxLevel);
        if (targetLevel <= character.Level)
        {
            return;
        }

        var currentLevelStartExperience = CharacterLevelProgression.GetTotalExperienceRequiredForLevel(character.Level);
        var requiredTotalExperience = CharacterLevelProgression.GetTotalExperienceRequiredForLevel(targetLevel);
        GainExperience(characterId, requiredTotalExperience - currentLevelStartExperience);
    }

    public void LearnAny(string characterId, string targetId, int level = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        var character = GetPartyMember(characterId);
        switch (ResolveLearnableKind(targetId, "learn"))
        {
            case LearnableKind.External: GrantExternalSkill(character, targetId, level); break;
            case LearnableKind.Internal: GrantInternalSkill(character, targetId, level); break;
            case LearnableKind.Special: LearnSpecialSkill(character, targetId); break;
            case LearnableKind.Talent: LearnTalent(character, targetId); break;
            case LearnableKind.Title: LearnTitle(character, targetId); break;
        }
    }

    public void LearnExternalSkill(string characterId, string skillId, int level = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        GrantExternalSkill(GetPartyMember(characterId), skillId, level);
    }

    public void LearnInternalSkill(string characterId, string skillId, int level = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        GrantInternalSkill(GetPartyMember(characterId), skillId, level);
    }

    public void LearnSpecialSkill(string characterId, string skillId) =>
        LearnSpecialSkill(GetPartyMember(characterId), skillId);

    public void LearnTalent(string characterId, string talentId) =>
        LearnTalent(GetPartyMember(characterId), talentId);

    public void LearnTitle(string characterId, string titleId) =>
        LearnTitle(GetPartyMember(characterId), titleId);

    public void EquipTitle(string characterId, string? titleId)
    {
        var character = GetPartyMember(characterId);
        if (character.EquipTitle(titleId)) PublishCharacterChanged(character);
    }

    public void GrantExternalSkill(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        var externalSkill = ContentRepository.GetExternalSkill(skillId);
        ApplyGrantedExternalSkill(character, externalSkill, ResolveAbsoluteSkillLevel(level));
    }

    public void StudyExternalSkillFromBook(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        var externalSkill = ContentRepository.GetExternalSkill(skillId);
        if (character.GetExternalSkillLevel(externalSkill.Id) is null &&
            character.GetExternalSkills().Count >= Config.MaxExternalSkillCount)
        {
            throw new InvalidOperationException("External skill count limit reached.");
        }

        ApplyGrantedExternalSkill(character, externalSkill, ResolveExternalSkillMaxLevel(externalSkill, level));
    }

    public void GrantInternalSkill(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        var internalSkill = ContentRepository.GetInternalSkill(skillId);
        ApplyGrantedInternalSkill(character, internalSkill, ResolveAbsoluteSkillLevel(level));
    }

    public void StudyInternalSkillFromBook(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        var internalSkill = ContentRepository.GetInternalSkill(skillId);
        if (character.GetInternalSkillLevel(internalSkill.Id) is null &&
            character.GetInternalSkills().Count >= Config.MaxInternalSkillCount)
        {
            throw new InvalidOperationException("Internal skill count limit reached.");
        }

        ApplyGrantedInternalSkill(character, internalSkill, ResolveInternalSkillMaxLevel(internalSkill, level));
    }

    public void UpgradeExternalSkillLevel(string characterId, string skillId, int levels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        var character = GetPartyMember(characterId);
        var definition = ContentRepository.GetExternalSkill(skillId);
        PublishSkillUpgradeResult(
            character,
            character.UpgradeExternalSkillLevel(definition, levels, ResolveAbsoluteSkillMaxLevel()),
            "外功");
    }

    public void UpgradeInternalSkillLevel(string characterId, string skillId, int levels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        var character = GetPartyMember(characterId);
        var definition = ContentRepository.GetInternalSkill(skillId);
        PublishSkillUpgradeResult(
            character,
            character.UpgradeInternalSkillLevel(definition, levels, ResolveAbsoluteSkillMaxLevel()),
            "内功");
    }

    public void UpgradeSkillLevel(string characterId, string skillId, int levels = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        if (ContentRepository.TryGetExternalSkill(skillId, out _))
        {
            UpgradeExternalSkillLevel(characterId, skillId, levels);
            return;
        }

        if (ContentRepository.TryGetInternalSkill(skillId, out _))
        {
            UpgradeInternalSkillLevel(characterId, skillId, levels);
            return;
        }

        throw new InvalidOperationException($"Unknown levelable skill '{skillId}'.");
    }

    public void LearnTalent(CharacterInstance character, string talentId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LearnTalent(ContentRepository.GetTalent(talentId)))
        {
            PublishToastAndCharacterChanged(
                character,
                $"{character.Name} 获得天赋【{talentId}】",
                ToastTone.Important);
        }
    }

    public void LearnTitle(CharacterInstance character, string titleId)
    {
        ArgumentNullException.ThrowIfNull(character);
        var title = ContentRepository.GetCharacterTitle(titleId);
        if (character.AddTitle(title))
        {
            PublishToastAndCharacterChanged(character, $"{character.Name} 获得称号【{title.Name}】", ToastTone.Important);
        }
    }

    public int GetTalentPointCapacity(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return CharacterTalentPointCalculator.CalculateCapacity(character, ResolveGrowTemplate(character));
    }

    public int GetSpentTalentPoints(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return CharacterTalentPointCalculator.CalculateSpentPoints(character);
    }

    public void LearnSpecialSkill(CharacterInstance character, string specialSkillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LearnSpecialSkill(ContentRepository.GetSpecialSkill(specialSkillId)))
        {
            PublishToastAndCharacterChanged(
                character,
                $"{character.Name} 习得特技【{specialSkillId}】",
                ToastTone.Important);
        }
    }

    public void RemoveAny(string characterId, string targetId)
    {
        var character = GetPartyMember(characterId);
        switch (ResolveLearnableKind(targetId, "remove"))
        {
            case LearnableKind.External: RemoveExternalSkill(character, targetId); break;
            case LearnableKind.Internal: RemoveInternalSkill(character, targetId); break;
            case LearnableKind.Special: RemoveSpecialSkill(character, targetId); break;
            case LearnableKind.Talent: RemoveTalent(character, targetId); break;
            case LearnableKind.Title: RemoveTitle(character, targetId); break;
        }
    }

    public void RemoveExternalSkill(string characterId, string skillId) =>
        RemoveExternalSkill(GetPartyMember(characterId), skillId);

    public void RemoveInternalSkill(string characterId, string skillId) =>
        RemoveInternalSkill(GetPartyMember(characterId), skillId);

    public void RemoveSpecialSkill(string characterId, string skillId) =>
        RemoveSpecialSkill(GetPartyMember(characterId), skillId);

    public void RemoveTalent(string characterId, string talentId) =>
        RemoveTalent(GetPartyMember(characterId), talentId);

    public void RemoveTitle(string characterId, string titleId) =>
        RemoveTitle(GetPartyMember(characterId), titleId);

    public void RemoveExternalSkill(CharacterInstance character, string skillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ContentRepository.GetExternalSkill(skillId);
        if (!character.RemoveExternalSkill(skillId)) return;

        PublishToastAndCharacterChanged(character, $"{character.Name} 移除外功【{skillId}】");
    }

    public void RemoveInternalSkill(CharacterInstance character, string skillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ContentRepository.GetInternalSkill(skillId);
        if (!character.RemoveInternalSkill(skillId)) return;

        PublishToastAndCharacterChanged(character, $"{character.Name} 移除内功【{skillId}】");
    }

    public void RemoveTalent(CharacterInstance character, string talentId)
    {
        ArgumentNullException.ThrowIfNull(character);

        ContentRepository.GetTalent(talentId);
        if (!character.RemoveTalent(talentId)) return;

        PublishToastAndCharacterChanged(character, $"{character.Name} 移除天赋【{talentId}】");
    }

    public void RemoveTitle(CharacterInstance character, string titleId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ContentRepository.GetCharacterTitle(titleId);
        if (!character.RemoveTitle(titleId)) return;
        PublishToastAndCharacterChanged(character, $"{character.Name} 移除称号【{titleId}】");
    }

    public void RemoveSpecialSkill(CharacterInstance character, string specialSkillId)
    {
        ArgumentNullException.ThrowIfNull(character);

        ContentRepository.GetSpecialSkill(specialSkillId);
        if (!character.RemoveSpecialSkill(specialSkillId)) return;

        PublishToastAndCharacterChanged(character, $"{character.Name} 移除特技【{specialSkillId}】");
    }

    public void SetExternalSkillActive(string characterId, string skillId, bool isActive)
    {
        var character = GetPartyMember(characterId);
        if (!character.SetExternalSkillActive(skillId, isActive))
        {
            return;
        }

        PublishCharacterChanged(character);
    }

    public void SetSpecialSkillActive(string characterId, string skillId, bool isActive)
    {
        var character = GetPartyMember(characterId);
        if (!character.SetSpecialSkillActive(skillId, isActive))
        {
            return;
        }

        PublishCharacterChanged(character);
    }

    public void SetFormSkillActive(string characterId, string sourceSkillId, string formSkillId, bool isActive)
    {
        var character = GetPartyMember(characterId);
        if (!character.SetFormSkillActive(sourceSkillId, formSkillId, isActive))
        {
            return;
        }

        PublishCharacterChanged(character);
    }

    public void EquipInternalSkill(string characterId, string skillId)
    {
        var character = GetPartyMember(characterId);
        if (!character.EquipInternalSkill(skillId))
        {
            return;
        }

        PublishCharacterChanged(character);
    }

    private void PublishSkillUpgradeResult<TSkill>(
        CharacterInstance character,
        SkillLevelChange<TSkill> change,
        string skillKind)
        where TSkill : SkillInstance
    {
        if (!change.Created && change.NewLevel == change.OldLevel)
        {
            return;
        }

        var message = change.Created
            ? $"{character.Name} 习得{skillKind}【{change.Skill.Name}】 {change.NewLevel}级"
            : $"{character.Name} {skillKind}【{change.Skill.Name}】 +{change.NewLevel - change.OldLevel}";
        PublishToastAndCharacterChanged(
            character,
            message,
            change.Created ? ToastTone.Important : ToastTone.Normal);
    }

    private void ApplyGrantedExternalSkill(CharacterInstance character, ExternalSkillDefinition externalSkill, int level)
    {
        if (character.GetExternalSkillLevel(externalSkill.Id) is int currentLevel &&
            currentLevel >= level)
        {
            return;
        }

        character.SetExternalSkillState(externalSkill, level, 0, true);
        PublishToastAndCharacterChanged(
            character,
            $"{character.Name} 习得外功【{externalSkill.Name}】 {level}级",
            ToastTone.Important);
    }

    private void ApplyGrantedInternalSkill(CharacterInstance character, InternalSkillDefinition internalSkill, int level)
    {
        if (character.GetInternalSkillLevel(internalSkill.Id) is int currentLevel &&
            currentLevel >= level)
        {
            return;
        }

        character.SetInternalSkillState(internalSkill, level, 0);
        PublishToastAndCharacterChanged(
            character,
            $"{character.Name} 习得内功【{internalSkill.Name}】 {level}级",
            ToastTone.Important);
    }

    private int ResolveExternalSkillMaxLevel(ExternalSkillDefinition externalSkill, int level) =>
        Math.Min(level, SkillMaxLevelPolicy.GetMaxLevel(externalSkill));

    private int ResolveInternalSkillMaxLevel(InternalSkillDefinition internalSkill, int level) =>
        Math.Min(level, SkillMaxLevelPolicy.GetMaxLevel(internalSkill));

    private int ResolveAbsoluteSkillLevel(int level)
    {
        return Math.Min(level, ResolveAbsoluteSkillMaxLevel());
    }

    private int ResolveAbsoluteSkillMaxLevel()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Config.AbsoluteSkillMaxLevel, 1);
        return Config.AbsoluteSkillMaxLevel;
    }

    private void PublishToastAndCharacterChanged(
        CharacterInstance character,
        string message,
        ToastTone tone = ToastTone.Normal)
    {
        _session.Events.Publish(new ToastRequestedEvent(message, tone));
        PublishCharacterChanged(character);
    }

    private void PublishCharacterChanged(CharacterInstance character) =>
        _session.Events.Publish(new CharacterChangedEvent(character.Id));

    private CharacterInstance GetPartyMember(string characterId)
    {
        if (State.Party.TryGetMember(characterId, out var character))
        {
            return character;
        }

        throw new InvalidOperationException($"Party member '{characterId}' does not exist.");
    }

    private LearnableKind ResolveLearnableKind(string targetId, string commandName)
    {
        if (ContentRepository.TryGetExternalSkill(targetId, out _)) return LearnableKind.External;
        if (ContentRepository.TryGetInternalSkill(targetId, out _)) return LearnableKind.Internal;
        if (ContentRepository.TryGetSpecialSkill(targetId, out _)) return LearnableKind.Special;
        if (ContentRepository.TryGetTalent(targetId, out _)) return LearnableKind.Talent;
        if (ContentRepository.TryGetCharacterTitle(targetId, out _)) return LearnableKind.Title;
        throw new InvalidOperationException($"Command '{commandName}' references unknown skill or talent '{targetId}'.");
    }

    private GrowTemplateDefinition ResolveGrowTemplate(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        var growTemplateId = character.GrowTemplateId ?? CharacterExperienceProgression.DefaultGrowTemplateId;
        return ContentRepository.GetGrowTemplate(growTemplateId);
    }

    private enum LearnableKind
    {
        External,
        Internal,
        Special,
        Talent,
        Title,
    }
}
