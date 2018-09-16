﻿using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class VirtualProductRepository : IVirtualProductRepository
    {
        private const string c_vpMappingCacheKey = "AllVirtualProductMappingsBy_ProjectId={0}";
        private const string c_vpCacheKey = "AllvirtualProductsBy_ProjectId_{0}";
        private readonly ICache m_cache;
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public VirtualProductRepository(ICache cache, IDatabase database, ISession session)
        {
            m_cache = cache;
            m_database = database;
            m_session = session;
        }

        public IEnumerable<IVirtualProduct> GetVirtualProductsByOrderItem(IPurchaseOrder order, IOrderItem item)
        {
            return GetVirtualProducts(order.ErpId, item.ErpProductId, item.PlacedName);
        }

        public IEnumerable<IVirtualProduct> GetVirtualProducts(int? erpId, string erpProductId, string placedName)
        {
            var allMappings = GetAllMappings();

            return allMappings.Where(m => IsMatch(m, erpId, erpProductId, placedName)).Select(m => m.VirtualProduct);
        }

        public void Map(int? erpId, string erpProductId, string placedName, int virtualProductId)
        {
            try
            {
                var allMappings = GetAllMappings();

                if (allMappings.Any(m => m.VirtualProductId == virtualProductId 
                                      && IsMatch(m, erpId, erpProductId, placedName)))
                {
                    return;
                }

                var newMapping = m_database.New<IVirtualProductOrderItemMapping>();
                newMapping.ErpId = erpId;
                newMapping.ErpProductId = erpProductId;
                newMapping.ItemName = placedName;
                newMapping.VirtualProductId = virtualProductId;
                newMapping.ProjectId = m_session.Project.Id;
                m_database.Save(newMapping);
            }
            finally
            {
                var key = string.Format(c_vpMappingCacheKey, m_session.Project.Id);
                m_cache.Remove(key);
            }
        }

        public IEnumerable<IVirtualProductMappableItem> GetMappableItems()
        {
            var result = new List<IVirtualProductMappableItem>();

            m_database.Sql().ExecuteWithParams(@"select distinct po.ErpId, oi.ErpProductId, oi.PlacedName
                                                         from OrderItem oi
                                                         inner join PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
                                                          where po.ProjectId = {0} 
                                                            AND po.PurchaseDate > DATEADD(year, -1, GETDATE())", 
                            m_session.Project.Id)
                           .ReadRows<int?, string, string>(
                    (erpId, erpProductId, placedName) =>
                        {
                            result.Add(new VpMappable
                                           {
                                               ErpId = erpId,
                                               ErpProductId = erpProductId,
                                               ItemName = placedName
                                           });
                        });

            return result;
        }

        public void Unmap(int? erpId, string erpProductId, string placedName, int virtualProductId)
        {
            try
            {
                var allMappings = GetAllMappings();

                var mappingToDelete =
                    allMappings.FirstOrDefault(
                        m => m.VirtualProductId == virtualProductId && IsMatch(m, erpId, erpProductId, placedName));

                if (mappingToDelete == null)
                {
                    return;
                }

                m_database.Delete(mappingToDelete);
            }
            finally
            {
                var key = string.Format(c_vpMappingCacheKey, m_session.Project.Id);
                m_cache.Remove(key);
            }
        }
        
        public IEnumerable<IVirtualProduct> GetAllVirtualProducts()
        {
            var cacheKey = string.Format(c_vpCacheKey, m_session.Project.Id);
            return m_cache.ReadThrough(cacheKey, TimeSpan.FromMinutes(10),
                () =>
                    {


                        return
                            m_database.SelectFrom<IVirtualProduct>()
                                .Where(p => p.ProjectId == m_session.Project.Id)
                                .Join(p => p.Materials)
                                .Execute();
                    });
        }

        private IEnumerable<IVirtualProductOrderItemMapping> GetAllMappings()
        {
            var key = string.Format(c_vpMappingCacheKey, m_session.Project.Id);
            return m_cache.ReadThrough(key, TimeSpan.FromMinutes(10), GetAllMappingsFromDatabase);
        }

        private IEnumerable<IVirtualProductOrderItemMapping> GetAllMappingsFromDatabase()
        {
            return
                m_database.SelectFrom<IVirtualProductOrderItemMapping>()
                    .Join(m => m.VirtualProduct)
                    .Join(m => m.VirtualProduct.Materials)
                    .Where(m => m.ProjectId == m_session.Project.Id)
                    .Where(m => m.VirtualProduct.ProjectId == m_session.Project.Id)
                    .Execute();
        }

        private static bool IsMatch(
            IVirtualProductOrderItemMapping mapping,
            int? erpId,
            string erpProductId,
            string placedName)
        {
            if ((mapping.ErpId ?? -1) != (erpId ?? -1))
            {
                return false;
            }

            if ((string.IsNullOrWhiteSpace(mapping.ErpProductId) != string.IsNullOrWhiteSpace(erpProductId)) || mapping.ErpProductId != erpProductId)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(mapping.ItemName) && mapping.ItemName != placedName)
            {
                return false;
            }

            return true;
        }

        private sealed class VpMappable : IVirtualProductMappableItem
        {
            public int? ErpId { get; set; }
            
            public string ErpProductId { get; set; }

            public string ItemName { get; set; }
        }
    }
}
