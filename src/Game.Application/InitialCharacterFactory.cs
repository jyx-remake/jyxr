using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class InitialCharacterFactory
{
    private readonly IContentRepository _contentRepository;
    private readonly GameConfig _config;

    public InitialCharacterFactory(IContentRepository contentRepository, GameConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        _contentRepository = contentRepository;
        _config = config ?? new GameConfig();
    }

    public CharacterInstance Create(string characterId, EquipmentInstanceFactory equipmentInstanceFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        ArgumentNullException.ThrowIfNull(equipmentInstanceFactory);

        var definition = _contentRepository.GetCharacter(characterId);
        var character = CharacterMapper.CreateInitial(characterId, definition, equipmentInstanceFactory, _config);
        if (_config.MaximizeNewPartyCharacterSkills)
        {
            character.LevelUpAllSkillsMaxLevel();
        }

        return character;
    }
}
