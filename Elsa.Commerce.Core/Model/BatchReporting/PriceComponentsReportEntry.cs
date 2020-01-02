using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class PriceComponentsReportEntry : BatchReportEntryBase
    {
        public PriceComponentsReportEntry(BatchKey batchKey) : base(batchKey)
        {
        }

        public List<PriceComponentModel> PriceComponents { get; } = new List<PriceComponentModel>();
    }
}
