using System;
using System.Collections.Generic;

using Elsa.Common.Configuration;
using Elsa.Integration.PaymentSystems.Common;

namespace Elsa.Integration.PaymentSystems.Fio
{
    [ConfigClass]
    public class FioClientConfig : IPaymentSystemCommonSettings
    {
        [ConfigEntry("FIO.Tokens", ConfigEntryScope.Project)]
        public Dictionary<string, string> Tokens { get; set; }

        [ConfigEntry("FIO.HistoryStart", ConfigEntryScope.Project)]
        public DateTime HistoryStart { get; set; }

        [ConfigEntry("FIO.BatchSizeDays", ConfigEntryScope.Project)]
        public int BatchSizeDays { get; set; }
    }
}
