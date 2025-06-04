using Elsa.Common.Configuration;
using System.Collections.Generic;

namespace Elsa.Commerce.Core.Configuration
{
    [ConfigClass]
    public class OrdersSystemConfig
    {
        [ConfigEntry("OrdersPacking.BatchSelectionValidityHours", ConfigEntryScope.User, ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int BatchPreferrenceLifetimeHours { get; set; }

        [ConfigEntry("OrdersPacking.MarkOrdersSentAsync", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public bool MarkOrdersSentAsync { get; set; }

        [ConfigEntry("OrdersPacking.FailureAdminMails", ConfigEntryScope.Project)]
        public List<string> PackingFailureAdminMails { get; set; }

        [ConfigEntry("OrderProcessing.UseWeight", "false", ConfigEntryScope.User, ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public bool UseOrderWeight { get; set; }

        [ConfigEntry("OrderProcessing.OrderWeightAddition", "0", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public decimal? OrderWeightAddition { get; set; }

        [ConfigEntry("OrderProcessing.PaymentMethodsToSetPaidAuto", "[]", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public List<string> PaymentMethodsToSetPaidAuto { get; set; }
    }
}
