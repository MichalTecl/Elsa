using Elsa.Common.Configuration;

namespace Elsa.Commerce.Core.Configuration
{
    [ConfigClass]
    public class PackingPreferredBatchesConfig
    {
        [ConfigEntry("OrdersPacking.BatchSelectionValidityHours", ConfigEntryScope.User, ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int BatchPreferrenceLifetimeHours { get; set; }
    }
}
