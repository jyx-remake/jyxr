using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class CharacterService
{
    private const string DefaultGrowTemplateId = "default";
    private const int LevelUpGrantedStatPoints = 2;
    private readonly GameSession _session;

    public CharacterService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public void RenameCharacter(string characterId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var character = GetPartyMember(characterId);
        character.Name = name;
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void SetCharacterPortrait(string characterId, string portrait)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portrait);
        var character = GetPartyMember(characterId);
        character.Portrait = portrait;
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void SetCharacterModel(string characterId, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        var character = GetPartyMember(characterId);
        character.Model = model;
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void SetGrowTemplate(string characterId, string growTemplateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(growTemplateId);
        var character = GetPartyMember(characterId);
        character.GrowTemplateId = growTemplateId;
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
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
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void AddBaseStat(string characterId, string statName, int value)
    {
        var character = GetPartyMember(characterId);
        character.AddBaseStat(StatCatalog.Parse(statName), value);
        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} {statName} {value:+0;-0;0}"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void ScaleLegacyMinusMaxPoints(string characterId, int tenths)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tenths);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tenths, 10);

        var character = GetPartyMember(characterId);
        var ratio = tenths / 10d;

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

        var currentPoints = character.UnspentStatPoints;
        var targetPoints = (int)(currentPoints * ratio);
        if (targetPoints != currentPoints)
        {
            character.SetUnspentStatPoints(targetPoints);
        }
        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 所有属性减半"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void AllocateStat(string characterId, StatType statType, int points = 1)
    {
        var character = GetPartyMember(characterId);
        character.AllocateStat(statType, points);
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void GrantStatPoints(string characterId, int points)
    {
        var character = GetPartyMember(characterId);
        character.GrantStatPoints(points);
        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 自由属性点 +{points}"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void GainExperience(string characterId, int experience)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(experience);
        if (experience == 0)
        {
            return;
        }

        var character = GetPartyMember(characterId);
        var oldLevel = character.Level;
        character.GrantExperience(experience);

        var resolvedLevel = Math.Max(oldLevel, CharacterLevelProgression.ResolveLevel(character.Experience));
        if (resolvedLevel > oldLevel)
        {
            ApplyLevelUps(character, oldLevel, resolvedLevel);
            _session.Events.Publish(new CharacterLeveledUpEvent(character.Id, oldLevel, resolvedLevel));
        }

        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void LevelUp(string characterId, int levels = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);

        var character = GetPartyMember(characterId);
        if (character.Level >= CharacterLevelProgression.MaxLevel)
        {
            return;
        }

        var targetLevel = Math.Min(character.Level + levels, CharacterLevelProgression.MaxLevel);
        if (targetLevel <= character.Level)
        {
            return;
        }

        var currentLevelStartExperience = CharacterLevelProgression.GetTotalExperienceRequiredForLevel(character.Level);
        var requiredTotalExperience = CharacterLevelProgression.GetTotalExperienceRequiredForLevel(targetLevel);
        GainExperience(characterId, requiredTotalExperience - currentLevelStartExperience);
    }

    public void Learn(string characterId, string learnType, string targetId, int level = 1)
    {
        var character = GetPartyMember(characterId);
        switch (learnType)
        {
            case "skill":
                LearnExternalSkill(character, targetId, level);
                return;
            case "internalskill" or "internal_skill":
                LearnInternalSkill(character, targetId, level);
                return;
            case "talent":
                LearnTalent(character, targetId);
                return;
            case "specialskill" or "special_skill":
                LearnSpecialSkill(character, targetId);
                return;
            default:
                throw new InvalidOperationException($"Unsupported learn type '{learnType}'.");
        }
    }

    public void LearnExternalSkill(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!ContentRepository.TryGetExternalSkill(skillId, out var externalSkill))
        {
            throw new InvalidOperationException($"Unknown external skill '{skillId}'.");
        }

        character.SetExternalSkillState(externalSkill, level, 0, true);
        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 习得外功【{skillId}】 {level}级"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void LearnInternalSkill(CharacterInstance character, string skillId, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!ContentRepository.TryGetInternalSkill(skillId, out var internalSkill))
        {
            throw new InvalidOperationException($"Unknown internal skill '{skillId}'.");
        }

        character.SetInternalSkillState(internalSkill, level, 0);
        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 习得内功【{skillId}】 {level}级"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void LearnTalent(CharacterInstance character, string talentId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LearnTalent(ContentRepository.GetTalent(talentId)))
        {
            _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 获得天赋【{talentId}】"));
            _session.Events.Publish(new CharacterChangedEvent(character.Id));
        }
    }

    public void LearnSpecialSkill(CharacterInstance character, string specialSkillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LearnSpecialSkill(ContentRepository.GetSpecialSkill(specialSkillId)))
        {
            _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 习得特技【{specialSkillId}】"));
            _session.Events.Publish(new CharacterChangedEvent(character.Id));
        }
    }

    public void Remove(string characterId, string removeType, string targetId)
    {
        var character = GetPartyMember(characterId);
        switch (removeType)
        {
            case "skill":
                RemoveExternalSkill(character, targetId);
                return;
            case "internalskill" or "internal_skill":
                RemoveInternalSkill(character, targetId);
                return;
            case "talent":
                RemoveTalent(character, targetId);
                return;
            case "specialskill" or "special_skill":
                RemoveSpecialSkill(character, targetId);
                return;
            default:
                throw new InvalidOperationException($"Unsupported remove type '{removeType}'.");
        }
    }

    public void RemoveExternalSkill(CharacterInstance character, string skillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!ContentRepository.TryGetExternalSkill(skillId, out _))
        {
            throw new InvalidOperationException($"Unknown external skill '{skillId}'.");
        }

        if (!character.RemoveExternalSkill(skillId)) return;

        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 移除外功【{skillId}】"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void RemoveInternalSkill(CharacterInstance character, string skillId)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!ContentRepository.TryGetInternalSkill(skillId, out _))
        {
            throw new InvalidOperationException($"Unknown internal skill '{skillId}'.");
        }

        if (!character.RemoveInternalSkill(skillId)) return;

        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 移除内功【{skillId}】"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void RemoveTalent(CharacterInstance character, string talentId)
    {
        ArgumentNullException.ThrowIfNull(character);

        ContentRepository.GetTalent(talentId);
        if (!character.RemoveTalent(talentId)) return;

        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 移除天赋【{talentId}】"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void RemoveSpecialSkill(CharacterInstance character, string specialSkillId)
    {
        ArgumentNullException.ThrowIfNull(character);

        ContentRepository.GetSpecialSkill(specialSkillId);
        if (!character.RemoveSpecialSkill(specialSkillId)) return;

        _session.Events.Publish(new ToastRequestedEvent($"{character.Name} 移除特技【{specialSkillId}】"));
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void SetExternalSkillActive(string characterId, string skillId, bool isActive)
    {
        var character = GetPartyMember(characterId);
        if (!character.SetExternalSkillActive(skillId, isActive))
        {
            return;
        }

        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void SetSpecialSkillActive(string characterId, string skillId, bool isActive)
    {
        var character = GetPartyMember(characterId);
        if (!character.SetSpecialSkillActive(skillId, isActive))
        {
            return;
        }

        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void EquipInternalSkill(string characterId, string skillId)
    {
        var character = GetPartyMember(characterId);
        if (!character.EquipInternalSkill(skillId))
        {
            return;
        }

        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    private CharacterInstance GetPartyMember(string characterId)
    {
        if (TryFindPartyMember(characterId, out var character))
        {
            return character;
        }

        throw new InvalidOperationException($"Party member '{characterId}' does not exist.");
    }

    private bool TryFindPartyMember(string characterId, out CharacterInstance character)
    {
        foreach (var member in State.Party.Members)
        {
            if (string.Equals(member.Id, characterId, StringComparison.Ordinal))
            {
                character = member;
                return true;
            }
        }

        character = null!;
        return false;
    }

    private void ApplyLevelUps(CharacterInstance character, int oldLevel, int newLevel)
    {
        var growTemplate = ResolveGrowTemplate(character);
        for (var currentLevel = oldLevel + 1; currentLevel <= newLevel; currentLevel += 1)
        {
            character.SetLevel(currentLevel);
            ApplyStatGrowth(character, growTemplate);
            character.GrantStatPoints(LevelUpGrantedStatPoints);
        }
    }

    private GrowTemplateDefinition ResolveGrowTemplate(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        var growTemplateId = character.GrowTemplateId ?? DefaultGrowTemplateId;
        return ContentRepository.GetGrowTemplate(growTemplateId);
    }

    private static void ApplyStatGrowth(CharacterInstance character, GrowTemplateDefinition growTemplate)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(growTemplate);

        foreach (var (statType, delta) in growTemplate.StatGrowth)
        {
            if (delta == 0)
            {
                continue;
            }

            character.AddBaseStat(statType, delta);
        }
    }
}
