using Game.Core.Model;

namespace Game.Application;

public interface IApplicationRuntimeHost
{
    ValueTask<InventoryEntry?> SelectRefinementEquipmentAsync(
        IReadOnlyList<InventoryEntry> entries,
        CancellationToken cancellationToken);
}
