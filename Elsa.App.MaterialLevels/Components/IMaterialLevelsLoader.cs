using System.Collections.Generic;
using Elsa.App.MaterialLevels.Components.Model;

namespace Elsa.App.MaterialLevels.Components
{
    public interface IMaterialLevelsLoader
    {
        IEnumerable<MaterialLevelEntryModel> Load(int inventoryId, string materialLevelReportingGroup);

        IEnumerable<InventoryModel> GetInventories();
    }
}
