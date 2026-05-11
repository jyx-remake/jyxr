using Game.Core.Affix;
using Game.Core.Definitions;

namespace Game.Core.Model;

public sealed class EquipmentInstance : IAffixProvider
{
    public EquipmentInstance(
        string id,
        EquipmentDefinition definition,
        IReadOnlyList<AffixDefinition>? extraAffixes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);

        Id = id;
        Definition = definition;
        ExtraAffixes = extraAffixes ?? [];
    }

    public string Id { get; }

    public EquipmentDefinition Definition { get; }

    public IReadOnlyList<AffixDefinition> ExtraAffixes { get; }

    public ProviderKind ProviderKind { get; } = ProviderKind.Equipment;

    public IReadOnlyList<AffixDefinition> Affixes => Definition.Affixes.Concat(ExtraAffixes).ToList();

    public bool HasInstanceLevelDifferences => ExtraAffixes.Count > 0;

    public IReadOnlyList<AffixDefinition> GetActiveAffixes() => Affixes;
}
