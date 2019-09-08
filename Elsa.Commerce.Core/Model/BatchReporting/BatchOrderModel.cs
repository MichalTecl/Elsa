namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchOrderModel
    {
        public long OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string Status { get; set; }

        public string PurchaseDate { get; set; }

        public string Customer { get; set; }
        public string Quantity { get; set; }
        public bool IsAllocation { get; set; }
        public string AllocationHandle { get; set; }
    }
}
