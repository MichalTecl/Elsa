using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class ShippingDetailModel
    {
        public string ShippingMethodName { get; set; }

        public decimal TaxedShippingCost { get; set; }

        public string OrderCurrencySymbol { get; set; }

        public string DpdUrl { get; set; }

        public string PacketaUrl { get; set; }

        public bool HasPackingInfo { get; set; }

        public string PackingUser { get; set; }

        public DateTime? PackingDt { get; set; }

        public ShippingAddressInfoModel DeliveryAddress { get; set; }
    }
}
