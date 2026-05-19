using Elsa.Apps.EshopMapping.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.EshopMapping.Internal
{
    public interface IEshopMappingFacade
    {
        List<EshopItemMappingRecord> GetMappings(int erpId, bool reloadErpProducts);
        List<EshopMappingLightRecord> GetLightMappings(int erpId, bool reloadErpProducts, bool includeHistoricalProducts);
        void Map(int erpId, string elsaMaterialName, string eshopProductName, bool deleteExistingMapping);
        void Unmap(int erpId, string elsaMaterialName, string eshopProductName);
        List<OrdersPeekModel> PeekOrders(string placedName, int erpId, bool inKits);
    }
}
