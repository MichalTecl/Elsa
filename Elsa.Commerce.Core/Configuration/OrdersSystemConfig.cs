using Elsa.Common.Configuration;

namespace Elsa.Commerce.Core.Configuration
{
    [ConfigClass]
    public class OrdersSystemConfig
    {
        [ConfigEntry("OrdersPacking.BatchSelectionValidityHours", ConfigEntryScope.User, ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int BatchPreferrenceLifetimeHours { get; set; }

        [ConfigEntry("OrdersPacking.MarkOrdersSentAsync", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public bool MarkOrdersSentAsync { get; set; }

        [ConfigEntry("OrderProcessing.UseWeight", "false", ConfigEntryScope.User, ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public bool UseOrderWeight { get; set; }
    }
}
