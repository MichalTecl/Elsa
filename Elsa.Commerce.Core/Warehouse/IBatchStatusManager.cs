using Elsa.Commerce.Core.Model;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IBatchStatusManager
    {
        IMaterialBatchStatus GetStatus(int batchId);
    }
}
