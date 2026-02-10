using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialFacade
    {
        IExtendedMaterialModel ProcessMaterialEditRequest(int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            bool automaticBatches,
            bool requiresPrice,
            bool requiresProductionPrice,
            bool requiresInvoice,
            bool requiresSupplierReference,
            bool autofinalize,
            bool canBeDigital,
            IEnumerable<string> components,
            string thresholdText,
            int? daysBeforeWarnForUnused,
            string unusedWarnMaterialType,
            bool usageProlongsLifetime, 
            bool notAbandonedUntilNewerBatchUsed,
            bool uniqueBatchNumbers,
            int? orderFulfillDays,
            int? expiratinMonths);
        
        MaterialSetupInfo GetMaterialInfo(string materialName);

        MaterialSetupInfo GetMaterialInfo(int materialId);

        IEnumerable<MaterialSetupInfo> GetAllMaterialInfo();
    }
}
