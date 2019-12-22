namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchSegmentModel
    {
        public int SegmentId { get; set; }

        public string Date { get; set; }

        public string Amount { get; set; }

        public string Author { get; set; }

        public string Price { get; set; }

        public bool HasRecipe { get; set; }
    }
}
