using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Configuration;
using Elsa.Integration.Erp.Flox;

namespace Elsa.Integration.Erp.Fler
{
    [ConfigClass]
    public class FlerClientConfig : IErpCommonSettings
    {
        public int MaxQueryDays => 100;

        public DateTime HistoryStart => DateTime.Now.AddDays(-100);

        public int OrderSyncHistoryDays => 100;

        [ConfigEntry("Fler.User", ConfigEntryScope.Project)]
        public string User { get; set; }

        [ConfigEntry("Fler.Password", ConfigEntryScope.Project)]
        public string Password { get; set; }

        [ConfigEntry("Fler.EnableWriteOperations", ConfigEntryScope.User, ConfigEntryScope.Project)]
        public bool EnableWriteOperations { get; set; }
    }
}
