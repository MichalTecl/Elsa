using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchReporting;

namespace Elsa.Commerce.Core.Warehouse.BatchReporting
{
    public interface IBatchReportingFacade
    {
        BatchMenuItems GetBatchMenuItems(BatchKey batchKey);

        BatchReportModel QueryBatches(BatchReportQuery query);
    }
}
