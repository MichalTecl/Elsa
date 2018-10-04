using Elsa.Common.Configuration;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    [ConfigClass]
    public class ZasilkovnaClientConfig
    {
        [ConfigEntry("Zasilkovna.ClientName", ConfigEntryScope.Project)]
        public string ClientName { get; set; }

        [ConfigEntry("Zasilkovna.ApiToken", ConfigEntryScope.Project)]
        public string ApiToken { get; set; }
    }
}
