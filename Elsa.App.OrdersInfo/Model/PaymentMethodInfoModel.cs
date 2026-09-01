using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class PaymentMethodInfoModel
    {
        public string Name { get; set; }

        public DateTime LastUsedDt { get; set; }

        public string LastUsedDtText { get; set; }

        public bool IsActive { get; set; }
    }
}
