using System;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportQuery
    {
        public string BatchId { get; set; }

        public int PageNumber { get; set; }
        
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
        
        public string ComponentId { get; set; }

        public string CompositionId { get; set; }
        
        public int? LoadOrdersPage { get; set; }

        public int? LoadSaleEventsPage { get; set; }

        public int? LoadSegmentsPage { get; set; }

        public long? RelativeToOrderId { get; set; }

        public bool BlockedBatchesOnly { get; set; }

        public int? SegmentId { get; set; }

        public bool HasKey => (!string.IsNullOrWhiteSpace(BatchNumberQuery) && MaterialId != null) || !string.IsNullOrWhiteSpace(BatchId);

        public BatchKey ToKey()
        {
            if (!string.IsNullOrWhiteSpace(BatchId))
            {
                return BatchKey.Parse(BatchId);
            }

            if (string.IsNullOrWhiteSpace(BatchNumberQuery) || MaterialId == null)
            {
                throw new InvalidOperationException("Cannot complete batch key");
            }

            return new BatchKey(MaterialId.Value, BatchNumberQuery);
        }
    }
}
