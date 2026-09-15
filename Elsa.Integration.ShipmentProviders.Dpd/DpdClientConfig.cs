using Elsa.Common.Configuration;

namespace Elsa.Integration.ShipmentProviders.Dpd
{
    [ConfigClass]
    public class DpdClientConfig
    {
        [ConfigEntry("Dpd.TrackingApiKey", ConfigEntryScope.Project)]
        public string TrackingApiKey { get; set; }
    }
}
