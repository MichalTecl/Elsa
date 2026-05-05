using System.Collections.Generic;
using Elsa.App.MaterialLevels.Components.Model;

namespace Elsa.App.MaterialLevels.Components
{
    public interface IInventoryWatchRepository
    {
        List<InventoryModel> GetVisibleTabs();

        void ShowTab(int inventoryId, string materialLevelReportingGroup);

        void HideTab(int inventoryId, string materialLevelReportingGroup);

        List<InventoryModel> GetHiddenTabs();
    }
}
