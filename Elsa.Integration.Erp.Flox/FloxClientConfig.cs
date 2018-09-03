using System;

using Elsa.Common.Configuration;

namespace Elsa.Integration.Erp.Flox
{
    [ConfigClass]
    public sealed class FloxClientConfig : IErpCommonSettings
    {
        [ConfigEntry("Flox.BaseUrl", ConfigEntryScope.Project)]
        public string Url { get; set; }

        [ConfigEntry("Flox.User", ConfigEntryScope.Project)]
        public string User { get; set; }

        [ConfigEntry("Flox.Password", ConfigEntryScope.Project)]
        public string Password { get; set; }

        [ConfigEntry("Flox.MaxQueryDays", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int MaxQueryDays { get; set; }

        [ConfigEntry("Flox.HistoryStart", ConfigEntryScope.Project)]
        public DateTime HistoryStart { get; set; }

        [ConfigEntry("Flox.OrderSyncHistoryDays", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int OrderSyncHistoryDays { get; set; }

        [ConfigEntry("Flox.EnableWriteOperations", ConfigEntryScope.User, ConfigEntryScope.Project)]
        public bool EnableWriteOperations { get; set; }
    }
}
