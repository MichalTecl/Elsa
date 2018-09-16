using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialRepository
    {
        IExtendedMaterialModel GetMaterialById(int materialId);

        IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId);
    }
}
