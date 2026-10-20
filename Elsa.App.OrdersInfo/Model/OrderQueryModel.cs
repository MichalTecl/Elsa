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

        public decimal? MinPriceWithVat { get; set; }

        public decimal? MaxPriceWithVat { get; set; }

        public List<string> ErpStatuses { get; set; }

        public int? OrderStatusId { get; set; }

        public string ContainsPlacedItemWildcard { get; set; }

        public string MaterialBatchNumberWildcard { get; set; }

        public string CustomerNameWildcard { get; set; }

        public string ShipmentMethodNameWildcard { get; set; }

        public List<string> PaymentMethodNames { get; set; }

        public string DiscountTextWildcard { get; set; }

        // Restricts orders to a cell of an aggregation (link from the aggregated table)
        public string DrilldownAggregationId { get; set; }

        public string DrilldownCell { get; set; }

        public Dictionary<string, string> DrilldownParams { get; set; }
    }
}
