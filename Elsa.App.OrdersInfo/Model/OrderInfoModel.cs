using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderInfoModel
    {
        public long OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal PriceWithVat { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string ErpStatusName { get; set; }

        public string ShippingMethodName { get; set; }

        public string PaymentMethodName { get; set; }

        public string CustomerName { get; set; }
    }
}
