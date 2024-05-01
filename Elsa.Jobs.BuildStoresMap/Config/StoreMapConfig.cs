using Elsa.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.BuildStoresMap.Config
{
    [ConfigClass]
    public class StoreMapConfig
    {
        [ConfigEntry("ValuableDistributor.MinOrdersCount", "2", ConfigEntryScope.Project)]
        public int MinOrdersCount { get; set; }

        [ConfigEntry("ValuableDistributor.MaxMonthsFromLastOrder", "5", ConfigEntryScope.Project)]
        public int MaxMonthsFromLastOrder { get; set; }
    }
}
