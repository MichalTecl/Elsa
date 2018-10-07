using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Integration.Erp.Flox;

namespace Elsa.Integration.Erp.Elerp
{
    public class ElerpConfig : IErpCommonSettings
    {
        public int MaxQueryDays { get; } = 1000;

        public DateTime HistoryStart { get; } = new DateTime(2017,01,01);

        public int OrderSyncHistoryDays { get; } = 1000;

        public string DataDir { get; set; } = "c:\\Elerp";
    }
}
