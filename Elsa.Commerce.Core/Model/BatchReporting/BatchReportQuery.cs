using System;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportQuery
    {
        public int PageNumber { get; set; }

        public int? MaterialId { get; set; }

        public int? InventoryTypeId { get; set; }

        public string OrderNumber { get; set; }

        public string BatchNumberQuery { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public bool? ClosedBatches { get; set; }

        public bool? LockedBatches { get; set; }
    }
}
