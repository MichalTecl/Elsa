using Elsa.Common.Configuration;

namespace Elsa.App.Commerce.Preview
{
    [ConfigClass]
    public class OverviewsConfig
    {
        [ConfigEntry("MissingPaymentsWidget.BusinessDaysTolerance", ConfigEntryScope.User, ConfigEntryScope.Project)]
        public int MissingPaymentDaysTolerance { get; set; }
    }
}
