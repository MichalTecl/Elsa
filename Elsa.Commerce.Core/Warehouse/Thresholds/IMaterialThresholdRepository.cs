using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.Warehouse.Thresholds
{
    public interface IMaterialThresholdRepository
    {
        IMaterialThreshold SaveThreshold(int materialId, decimal value, int unitId);

        IEnumerable<IMaterialThreshold> GetAllThresholds();

        IMaterialThreshold GetThreshold(int materialId);

        void DeleteThreshold(int materialId);

        bool HasThreshold(int materialId);
    }
}
