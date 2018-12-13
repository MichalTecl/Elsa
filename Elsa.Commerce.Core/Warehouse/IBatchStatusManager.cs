using Elsa.Commerce.Core.Model;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IBatchStatusManager
    {
        IMaterialBatchStatus GetStatus(int batchId);

        void OnBatchChanged(int batchId);

        //void NotifyProductionStepChanged(int batchId);

        //void NotifyBatchUsageInStep(int batchId);

        //void NotifyBatchEvent(int batchId);

        //void NotifyComposition(int batchId);

        //void NotifyBatchOrderUsage(int batchId);
    }
}
