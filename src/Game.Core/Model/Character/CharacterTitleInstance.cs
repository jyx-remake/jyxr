using Game.Core.Definitions;

namespace Game.Core.Model.Character;

public sealed class CharacterTitleInstance
{
    public CharacterTitleInstance(CharacterTitleDefinition definition, bool equipped = false)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Equipped = equipped;
    }
    public CharacterTitleDefinition Definition { get; }
    public string Id => Definition.Id;
    public bool Equipped { get; private set; }
    public void SetEquipped(bool equipped) => Equipped = equipped;
}
