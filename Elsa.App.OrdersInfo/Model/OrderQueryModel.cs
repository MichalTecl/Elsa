using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderQueryModel
    {
        public int PageSize { get; set; }

        public int Page { get; set; }

        public string OrderNumber { get; set; }

        public DateTime? MinPurchaseDt { get; set; }

        public DateTime? MaxPurchaseDt { get; set; }

        public List<string> ErpStatuses { get; set; }

        public string ContainsPlacedItemWildcard { get; set; }

        public string CustomerNameWildcard { get; set; }

        public string ShipmentMethodNameWildcard { get; set; }

        public string PaymentMethodNameWildcard { get; set; }
    }
}
