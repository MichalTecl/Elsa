using System;
using System.Collections.Generic;

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

        public decimal TotalItemsWeightKg { get; set; }

        public List<ShippingItemWeightModel> ItemWeights { get; set; }
    }

    public class ShippingItemWeightModel
    {
        public string ItemName { get; set; }

        public decimal? WeightKg { get; set; }
    }
}
