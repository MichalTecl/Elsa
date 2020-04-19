using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.App.MaterialLevels.Components
{
    public interface IInventoryWatchRepository
    {
        List<IMaterialInventory> GetWatchedInventories();

        void WatchInventory(int inventoryId);

        void UnwatchInventory(int inventoryId);

        List<IMaterialInventory> GetUnwatchedInventories();
    }
}
