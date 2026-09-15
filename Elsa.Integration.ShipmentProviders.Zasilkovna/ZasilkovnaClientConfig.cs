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

        [ConfigEntry("Zasilkovna.WebLoginUserName", ConfigEntryScope.Project)]
        public string WebLoginUserName { get; set; }

        [ConfigEntry("Zasilkovna.WebLoginPassword", ConfigEntryScope.Project)]
        public string WebLoginPassword { get; set; }
    }
}
