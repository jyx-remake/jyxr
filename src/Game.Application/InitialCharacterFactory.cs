using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class InitialCharacterFactory
{
    private readonly IContentRepository _contentRepository;
    private readonly GameConfig _config;
    private readonly SkillMaxLevelPolicy _skillMaxLevelPolicy;

    public InitialCharacterFactory(
        IContentRepository contentRepository,
        GameConfig? config = null,
        SkillMaxLevelPolicy? skillMaxLevelPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        _contentRepository = contentRepository;
        _config = config ?? new GameConfig();
        _skillMaxLevelPolicy = skillMaxLevelPolicy ?? new SkillMaxLevelPolicy(_config);
    }

    public CharacterInstance Create(string characterId, EquipmentInstanceFactory equipmentInstanceFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        ArgumentNullException.ThrowIfNull(equipmentInstanceFactory);

        var definition = _contentRepository.GetCharacter(characterId);
        var character = CharacterMapper.CreateInitial(characterId, definition, equipmentInstanceFactory, _config);
        if (_config.MaximizeNewPartyCharacterSkills)
        {
            character.LevelUpAllSkillsMaxLevel(
                _skillMaxLevelPolicy.GetMaxLevel,
                _skillMaxLevelPolicy.GetMaxLevel);
        }

        return character;
    }
}
