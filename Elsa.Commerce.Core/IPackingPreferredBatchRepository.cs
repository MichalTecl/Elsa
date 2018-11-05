using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core
{
    public interface IPackingPreferredBatchRepository
    {
        IEnumerable<IPackingPreferredBatch> GetPreferredBatches();

        void SetBatchPreferrence(int batchId);

        void NotifyPreferrenceActivity(int preferrenceId);

        void InvalidatePreferrence(int preferrenceId);
    }
}
