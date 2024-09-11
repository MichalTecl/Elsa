using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Text;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportQuery
    {
        public string BatchId { get; set; }

        public int PageNumber { get; set; }
        
        public int? MaterialId { get; set; }

        public int? InventoryTypeId { get; set; }

        public string OrderNumber { get; set; }

        public string InvoiceNr { get; set; }

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

        public int? LoadPriceComponentsPage { get; set; }

        public long? RelativeToOrderId { get; set; }

        public bool BlockedBatchesOnly { get; set; }

        public int? SegmentId { get; set; }

        public bool HasKey => (!string.IsNullOrWhiteSpace(BatchNumberQuery) && MaterialId != null) || !string.IsNullOrWhiteSpace(BatchId);

        public bool LoadBatchDetails { get; set; }

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
                
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"PageNumber:{PageNumber}, ");
            sb.Append($"LoadBatchDetails:{LoadBatchDetails}, ");

            if (!string.IsNullOrEmpty(BatchId)) { sb.Append($"BatchId:{BatchId}, "); }
            if (null != MaterialId) { sb.Append($"MaterialId:{MaterialId}, "); }
            if (null != InventoryTypeId) { sb.Append($"InventoryTypeId:{InventoryTypeId}, "); }
            if (!string.IsNullOrEmpty(OrderNumber)) { sb.Append($"OrderNumber:{OrderNumber}, "); }
            if (!string.IsNullOrEmpty(InvoiceNr)) { sb.Append($"InvoiceNr:{InvoiceNr}, "); }
            if (!string.IsNullOrEmpty(BatchNumberQuery)) { sb.Append($"BatchNumberQuery:{BatchNumberQuery}, "); }
            if (null != From) { sb.Append($"From:{From}, "); }
            if (null != To) { sb.Append($"To:{To}, "); }
            if (null != ClosedBatches) { sb.Append($"ClosedBatches:{ClosedBatches}, "); }
            if (null != LockedBatches) { sb.Append($"LockedBatches:{LockedBatches}, "); }
            if (ProducedOnly) { sb.Append($"ProducedOnly:{ProducedOnly}, "); }
            if (PurchasedOnly) { sb.Append($"PurchasedOnly:{PurchasedOnly}, "); }
            if (!string.IsNullOrEmpty(ComponentId)) { sb.Append($"ComponentId:{ComponentId}, "); }
            if (!string.IsNullOrEmpty(CompositionId)) { sb.Append($"CompositionId:{CompositionId}, "); }
            if (null != LoadOrdersPage) { sb.Append($"LoadOrdersPage:{LoadOrdersPage}, "); }
            if (null != LoadSaleEventsPage) { sb.Append($"LoadSaleEventsPage:{LoadSaleEventsPage}, "); }
            if (null != LoadSegmentsPage) { sb.Append($"LoadSegmentsPage:{LoadSegmentsPage}, "); }
            if (null != LoadPriceComponentsPage) { sb.Append($"LoadPriceComponentsPage:{LoadPriceComponentsPage}, "); }
            if (null != RelativeToOrderId) { sb.Append($"RelativeToOrderId:{RelativeToOrderId}, "); }
            if (BlockedBatchesOnly) { sb.Append($"BlockedBatchesOnly:{BlockedBatchesOnly}, "); }
            if (null != SegmentId) { sb.Append($"SegmentId:{SegmentId}, "); }

            return sb.ToString();
        }
    }
}
