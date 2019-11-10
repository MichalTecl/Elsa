using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialFacade
    {
        IExtendedMaterialModel ProcessMaterialEditRequest(
            int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            bool automaticBatches,
            bool requiresPrice,
            bool requiresInvoice,
            bool requiresSupplierReference,
            IEnumerable<string> components,
            string thresholdText);
        
        MaterialSetupInfo GetMaterialInfo(string materialName);

        IEnumerable<MaterialSetupInfo> GetAllMaterialInfo();
    }
}
