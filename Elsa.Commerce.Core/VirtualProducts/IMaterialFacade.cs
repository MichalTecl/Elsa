using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialFacade
    {
        IExtendedMaterialModel ProcessMaterialEditRequest(MaterialEditRequestModel request);

        MaterialSetupInfo GetMaterialInfo(string materialName);

        MaterialSetupInfo GetMaterialInfo(int materialId);

        IEnumerable<MaterialSetupInfo> GetAllMaterialInfo();
    }
}
