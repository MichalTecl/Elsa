using System;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportQuery
    {
        public int PageNumber { get; set; }

        public int? BatchId { get; set; }
        
        public int? MaterialId { get; set; }

        public int? InventoryTypeId { get; set; }

        public string OrderNumber { get; set; }

        public string BatchNumberQuery { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public bool? ClosedBatches { get; set; }

        public bool? LockedBatches { get; set; }

        public bool ProducedOnly { get; set; }

        public bool PurchasedOnly { get; set; }

        public int? ComponentId { get; set; }

        public int? CompositionId { get; set; }

        public bool LoadSteps { get; set; }

        public int? LoadOrdersPage { get; set; }

        public long? RelativeToOrderId { get; set; }

        public bool BlockedBatchesOnly { get; set; }
    }
}
