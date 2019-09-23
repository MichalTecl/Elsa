using System.Collections.Generic;

namespace Elsa.App.SaleEvents.Model
{
    public class SaleEventsCollection 
    {
        public SaleEventsCollection(int? nextPageNumber, IEnumerable<SaleEventViewModel> events)
        {
            NextPageNumber = nextPageNumber;
            HasNextPage = nextPageNumber != null;
            Events = new List<SaleEventViewModel>(events);
        }

        public bool HasNextPage { get; set; }

        public int? NextPageNumber { get; set; }

        public List<SaleEventViewModel> Events { get; }
    }
}
