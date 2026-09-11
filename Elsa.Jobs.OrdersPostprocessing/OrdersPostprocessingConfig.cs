using Elsa.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.OrdersPostprocessing
{
    [ConfigClass]
    public class OrdersPostprocessingConfig
    {
        [ConfigEntry("OrdersPostprocessing.PaymentIbanCzk", ConfigEntryScope.Project)]
        public string OrdersPaymentIbanCzk { get; set; }

        [ConfigEntry("OrdersPostprocessing.AutomaticPaymentRemindersEnabled", ConfigEntryScope.Project)]
        public bool AutomaticPaymentRemindersEnabled { get; set; }
    }
}
