using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialFacade
    {
        IExtendedMaterialModel ProcessMaterialEditRequest(
            int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            IEnumerable<string> components);
    }
}
