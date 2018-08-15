using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IOrderStatusMappingRepository
    {
        IDictionary<string, IErpOrderStatusMapping> GetMappings(int erpId);
    }
}
