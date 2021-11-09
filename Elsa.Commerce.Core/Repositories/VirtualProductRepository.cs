using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
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
        private readonly ILog m_log;

        public VirtualProductRepository(ICache cache, IDatabase database, ISession session, ILog log)
        {
            m_cache = cache;
            m_database = database;
            m_session = session;
            m_log = log;
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

                if (allMappings.Any(m => (m.VirtualProductId == virtualProductId) 
                                      && IsMatch(m, erpId, erpProductId, placedName)))
                {
                    return;
                }

                var newMapping = m_database.New<IVirtualProductOrderItemMapping>();
                //newMapping.ErpId = erpId;
                //newMapping.ErpProductId = erpProductId;
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
                        m => (m.VirtualProductId == virtualProductId) && IsMatch(m, erpId, erpProductId, placedName));

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

        public IVirtualProduct GetVirtualProductById(int id)
        {
            return GetAllVirtualProducts().FirstOrDefault(i => i.Id == id);
        }

        public IVirtualProduct CreateOrUpdateVirtualProduct(int? virtualProductId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tag musí mít název");
            }

            if (name.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"Nepovolený název tagu \"{name}\" - tag nesmí obsahovat mezery ani jiné bílé znaky");
            }

            IVirtualProduct vp;
            if (virtualProductId == null)
            {
                var existing =
                    m_database.SelectFrom<IVirtualProduct>()
                        .Where(v => v.ProjectId == m_session.Project.Id)
                        .Where(v => v.Name == name)
                        .Execute()
                        .FirstOrDefault();

                if (existing != null)
                {
                    throw new ArgumentException($"Název tagu musí být jedinečný, již existuje \"{existing.Name}\"");
                }

                vp = m_database.New<IVirtualProduct>();
                vp.ProjectId = m_session.Project.Id;
            }
            else
            {
                vp = GetVirtualProductById(virtualProductId.Value);
                if (vp == null)
                {
                    throw new InvalidOperationException("Invalid VirtualProductId");
                }

                if (vp.Name == name)
                {
                    return vp;
                }
            }

            vp.Name = name;
            m_database.Save(vp);
            CleanCache();

            return vp;
        }

        public void CleanCache()
        {
            m_cache.Remove(string.Format(c_vpMappingCacheKey, m_session.Project.Id));
            m_cache.Remove(string.Format(c_vpCacheKey, m_session.Project.Id));
        }

        public void DeleteVirtualProduct(int vpId)
        {
            
            var prid = m_session.Project.Id;

            using (var tx = m_database.OpenTransaction())
            {
                var vp =
                    m_database.SelectFrom<IVirtualProduct>()
                        .Where(p => p.Id == vpId)
                        .Where(p => p.ProjectId == prid)
                        .Execute()
                        .FirstOrDefault();

                if (vp == null)
                {
                    m_log.Info($"VirtualProductId = {vpId} not found");
                    return;
                }

                var materialMappings =
                    m_database.SelectFrom<IVirtualProductMaterial>()
                        .Join(m => m.Component)
                        .Where(m => m.Component.ProjectId == prid)
                        .Where(m => m.VirtualProductId == vpId)
                        .Execute();

                m_database.DeleteAll(materialMappings);

                var itemMappings =
                    m_database.SelectFrom<IVirtualProductOrderItemMapping>()
                        .Where(m => m.ProjectId == prid)
                        .Where(m => m.VirtualProductId == vpId)
                        .Execute();

                m_database.DeleteAll(itemMappings);

                m_database.Delete(vp);
                
                tx.Commit();
                CleanCache();
            }
        }

        public IDisposableVirtualProductsRepository GetWithPostponedCache()
        {
            return new WithPostopnedCacheRemoval(new CacheWithPostponedRemoval(m_cache), m_database, m_session, m_log);
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
                    .Join(m => m.VirtualProduct.Materials.Each().Unit)
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
            /*
            if ((mapping.ErpId ?? -1) != (erpId ?? -1))
            {
                return false;
            }

            if ((string.IsNullOrWhiteSpace(mapping.ErpProductId) != string.IsNullOrWhiteSpace(erpProductId)) || mapping.ErpProductId != erpProductId)
            {
                return false;
            }
            */

            if (!string.IsNullOrWhiteSpace(mapping.ItemName) && (mapping.ItemName != placedName))
            {
                return false;
            }

            return true;
        }

        public List<ErpProductMapping> ExportErpProductMappings()
        {
            return m_database.Sql().ExecuteWithParams(@"
            select vpim.ItemName, m.Name
              from VirtualProductOrderItemMapping vpim
              join VirtualProduct vp ON (vpim.VirtualProductId = vp.Id)
              join VirtualProductMaterial vpm ON (vp.Id = vpm.VirtualProductId)
              join Material m ON (vpm.ComponentId = m.Id)
              join MaterialInventory mi ON (m.InventoryId = mi.Id)  
              where vpim.ProjectId = {0}
              order by vpim.ItemName, m.Name",
            m_session.Project.Id)
                .MapRows<ErpProductMapping>(row => new ErpProductMapping { EshopItem = row.GetString(0), Material = row.GetString(1) }).ToList();
        }

        public void ImportErpProductMappings(List<ErpProductMapping> mappings)
        {            
            var current = ExportErpProductMappings();

            m_log.Info($"Existing product mappings = {current.Count}, import length = {mappings.Count}");

            var toProcess = mappings.Distinct()
                .Where(m => !(string.IsNullOrWhiteSpace(m.EshopItem) && string.IsNullOrWhiteSpace(m.Material)))
                .Where(m => !current.Any(c => c.EshopItem == m.EshopItem && c.Material == m.Material))
                .ToList();

            m_log.Info($"After filtering and removal of already existing entries there is {toProcess.Count} of records to be imported");

            if (toProcess.Count == 0)
                return;

            using (var tx = m_database.OpenTransaction())
            {
                foreach(var imp in toProcess)
                {
                    try
                    {
                        m_log.Info($"Going to import mapping {imp.EshopItem} -> {imp.Material}");

                        var tag = Tagify(imp.EshopItem);

                        var res = m_database.Sql()
                            .Call("sp_assignEshopItemToMaterial")
                            .WithParam("@placedName", imp.EshopItem)
                            .WithParam("@materialName", imp.Material)
                            .WithParam("@tagName", tag)
                            .WithParam("@projectId", m_session.Project.Id)
                            .Scalar<string>();

                        if (!string.IsNullOrWhiteSpace(res))
                            throw new Exception(res);

                        m_log.Info($"Successfully imported mapping {imp.EshopItem} -> {imp.Material} (tag: {tag})");
                    }
                    catch (Exception ex)
                    {
                        m_log.Error($"An attempt to import {imp.EshopItem} -> {imp.Material} failed", ex);
                        throw;
                    }
                }

                tx.Commit();

                m_log.Info($"Done");
            }
        }

        private static string Tagify(string itemName)
        {
            const string allowedChars = "abcdefghijklmnopqrstuvwxyz1234567890";
            itemName = itemName.ToLowerInvariant();

            IEnumerable<char> tf()
            {                                
                foreach(var chr in itemName)
                {
                    if (allowedChars.IndexOf(chr) < 0)
                        yield return '_';
                    else
                        yield return chr;
                }
            }

            var tag = $"PROD_{string.Join(null, tf())}";

            tag = tag.TrimEnd('_');

            while (tag.Contains("__"))
                tag = tag.Replace("__", "_");

            if (tag.Length > 255) 
            {                
                tag = tag.Substring(0, 200).TrimEnd('_');
                tag = $"{tag}_{Guid.NewGuid(): N}";
            }

            return tag;
        }

        private sealed class VpMappable : IVirtualProductMappableItem
        {
            public int? ErpId { get; set; }
            
            public string ErpProductId { get; set; }

            public string ItemName { get; set; }
        }

        private sealed class WithPostopnedCacheRemoval : VirtualProductRepository, IDisposableVirtualProductsRepository
        {
            private readonly CacheWithPostponedRemoval m_ppCache;

            public WithPostopnedCacheRemoval(CacheWithPostponedRemoval cache, IDatabase database, ISession session, ILog log) : base(cache, database, session, log)
            {
                m_ppCache = cache;
            }
            
            public void Dispose()
            {
                m_ppCache.Dispose();
            }
        }
    }
}

