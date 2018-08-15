using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IProductMappingRepository
    {
        IDictionary<string, IErpProductMapping> GetMappings(int erpId);
    }
}
