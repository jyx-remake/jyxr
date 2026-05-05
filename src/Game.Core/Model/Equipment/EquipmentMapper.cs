using Game.Core.Abstractions;
using Game.Core.Persistence;

namespace Game.Core.Model;

public static class EquipmentMapper
{
    public static EquipmentInstance FromRecord(EquipmentRecord record, IContentRepository contentRepository)
    {
        var definition = contentRepository.GetEquipment(record.EquipmentDefinitionId);
        foreach (var affix in record.ExtraAffixes)
        {
            affix.Resolve(contentRepository);
        }

        return new EquipmentInstance(record.Id, definition, record.ExtraAffixes);
    }

    public static EquipmentRecord ToRecord(EquipmentInstance equipment) =>
        new(equipment.Id, equipment.Definition.Id, equipment.ExtraAffixes);
}
