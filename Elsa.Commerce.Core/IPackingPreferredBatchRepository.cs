using Elsa.Commerce.Core.Model;

namespace Elsa.Commerce.Core
{
    public interface IPackingPreferredBatchRepository
    {
        string GetPrefferedBatchNumber(int materialId);

        void InvalidatePreferrenceByMaterialId(int materialId);
        
        void SetBatchPreferrence(BatchKey batchKey);
    }
}
