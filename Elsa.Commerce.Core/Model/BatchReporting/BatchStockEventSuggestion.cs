namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchStockEventSuggestion
    {
        public string Id { get { return Title; } }
        public string Title { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string BatchNumber { get; set; }
        public int EventTypeId { get; set; }
        public string UnitSymbol { get; set; }
        public decimal? Amount { get; set; }

    }
}
