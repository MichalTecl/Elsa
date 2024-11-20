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

        [ConfigEntry("Flox.PreferApi", "false", ConfigEntryScope.Project)]
        public bool PreferApi { get; set; }

        [ConfigEntry("Flox.ApiKey", ConfigEntryScope.Project)]
        public string ApiKey { get; set; }

        [ConfigEntry("Flox.UseIncrementalOrderChangeMode", "false", ConfigEntryScope.Project)]
        public bool UseIncrementalOrderChangeMode { get; set; }

        [ConfigEntry("Flox.IncrementalModeMaxHistoryDays", "0", ConfigEntryScope.Project)]
        public int IncrementalModeMaxHistoryDays { get; set; }

        [ConfigEntry("Flox.PaidOrdersSyncHistoryDays", "30", ConfigEntryScope.Project)]
        public int PaidOrdersSyncHistoryDays { get; set; }

        [ConfigEntry("Flox.ApiUrl", ConfigEntryScope.Project)]
        public string ApiUrl { get; set; }
    }
}
