using Elsa.Commerce.Core.Model.BatchReporting;

namespace Elsa.Commerce.Core.Warehouse.BatchReporting
{
    public interface IBatchReportingFacade
    {
        BatchReportModel QueryBatches(BatchReportQuery query);
    }
}
