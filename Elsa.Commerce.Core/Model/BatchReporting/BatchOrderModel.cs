namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchOrderModel
    {
        public long OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string Status { get; set; }

        public string PurchaseDate { get; set; }

        public string Customer { get; set; }
    }
}
