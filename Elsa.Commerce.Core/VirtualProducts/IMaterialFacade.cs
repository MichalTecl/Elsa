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
            IEnumerable<string> components);
    }
}
