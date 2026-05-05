using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class EquipmentInstanceFactory
{
    private long _nextNumber = 1;

    public long NextNumber => _nextNumber;

    public static EquipmentInstanceFactory Restore(long nextNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nextNumber, 1);

        return new EquipmentInstanceFactory
        {
            _nextNumber = nextNumber,
        };
    }

    public EquipmentInstance Create(
        EquipmentDefinition definition,
        IReadOnlyList<AffixDefinition>? extraAffixes = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new EquipmentInstance(NextId(definition), definition, extraAffixes);
    }

    public EquipmentInstanceFactoryRecord ToRecord() => new(_nextNumber);

    private string NextId(EquipmentDefinition definition) => $"{definition.Id}_{_nextNumber++:D8}";
}
