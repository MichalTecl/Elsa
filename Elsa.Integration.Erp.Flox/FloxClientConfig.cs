using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
