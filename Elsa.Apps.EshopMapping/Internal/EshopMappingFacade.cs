using Elsa.Apps.EshopMapping.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Apps.EshopMapping.Internal
{
    public class EshopMappingFacade : IEshopMappingFacade
    {
        private static readonly int _abandonedHoursLimit = 24 * 31 * 6; // ~ 6 months

        private readonly IWebSession _session;
        private readonly IVirtualProductRepository _vpRepo;
        private readonly IErpClientFactory _erpFactory;
        private readonly IMaterialRepository _materialRepo;
        private readonly IDatabase _db;
        private readonly ICache _cache;

        public EshopMappingFacade(IWebSession session, IVirtualProductRepository vpRepo, IErpClientFactory erpFactory, IMaterialRepository materialRepo, IDatabase db, ICache cache)
        {
            _session = session;
            _vpRepo = vpRepo;
            _erpFactory = erpFactory;
            _materialRepo = materialRepo;
            _db = db;
            _cache = cache;
        }

        public List<EshopItemMappingRecord> GetMappings(int erpId, bool reloadErpProducts)
        { 
            var erpProductListCacheKey = $"erpProductList_{_session.Project.Id}_{erpId}";
            if (reloadErpProducts)
            {
                _cache.Remove(erpProductListCacheKey);
            }

            var erpProducts = _cache.ReadThrough(erpProductListCacheKey, TimeSpan.FromHours(1), () =>
            {
                var erp = _erpFactory.GetErpClient(erpId);
                return erp.GetProductNames();
            });

            var dbMappings = _vpRepo.ExportErpProductMappings().GroupBy(m => m.Material);

            var orderingInfo = _cache.ReadThrough($"productOrderingInfo_{_session.Project.Id}_{erpId}",
                TimeSpan.FromMinutes(5), 
                () => _db.Sql().Call("GetProductOrderingInfo").WithParam("@erpId", erpId).AutoMap<ProductOrderingInfo>().ToDictionary(oi => oi.PlacedName, oi => oi));

            var productsInventory = _materialRepo.GetMaterialInventories().FirstOrDefault(i => i.CanBeConnectedToTag).Ensure();
            var elsaProducts = _materialRepo.GetAllMaterials(productsInventory.Id, false).ToList();
                        
            var result = new List<EshopItemMappingRecord>(elsaProducts.Count);
            foreach(var material in elsaProducts.OrderBy(p => p.Name))
            {
                var record = new EshopItemMappingRecord();
                result.Add(record);

                record.ElsaMaterialName = material.Name;

                // 1. does mapping exist?
                var existingMappings = dbMappings.FirstOrDefault(m => m.Key == material.Name);
                if (existingMappings != null)
                {
                    foreach(var mapping in existingMappings)
                    {
                        var mappedProd = new MappedProduct
                        {
                            ProductName = mapping.EshopItem
                        };

                        record.Products.Add(mappedProd);

                        // So we are not creating another record for this eshop product (while because in theory there could be more than one with the same name)
                        while (erpProducts.Remove(mapping.EshopItem)) 
                            mappedProd.ErpProductExists = true;
                    }
                }
            }

            // Erp products without mapping
            foreach (var erpProd in erpProducts.Distinct())
            {
                var rec = new EshopItemMappingRecord();
                result.Add(rec);
                rec.Products.Add(new MappedProduct
                {
                    ProductName = erpProd,
                    ErpProductExists = true
                });
            }

            // Try to bind info about orders

            foreach(var r in result)
                foreach(var p in r.Products)
                    if (orderingInfo.TryGetValue(p.ProductName, out var info))
                    {
                        p.OrderCount = info.OrderCount;
                        p.LastOrderedAt = StringUtil.FormatDate(info.LastOrder);
                        p.SeemsAbandoned = ((DateTime.Now - info.LastOrder).TotalHours > _abandonedHoursLimit);
                    }
                
            return result;
        }

        public void Map(int erpId, string elsaMaterialName, string eshopProductName, bool deleteExistingMapping)
        {
            var currentMappings = GetMappings(erpId, false);
            var existingMapping = currentMappings.FirstOrDefault(m => !string.IsNullOrEmpty(m.ElsaMaterialName) && m.Products.Any(p => p.ProductName == eshopProductName));
            if (existingMapping != null)
            {
                if (deleteExistingMapping)
                {
                    Unmap(erpId, elsaMaterialName, eshopProductName);
                    Map(erpId, elsaMaterialName, eshopProductName, false);
                    return;
                }

                throw new InvalidOperationException($"Produkt \"{eshopProductName}\" již je propojen s materiálem \"{existingMapping.ElsaMaterialName}\"");
            }

            _vpRepo.ImportErpProductMappings(new List<Commerce.Core.VirtualProducts.Model.ErpProductMapping>
            {
                new Commerce.Core.VirtualProducts.Model.ErpProductMapping
                {
                    EshopItem = eshopProductName,
                    Material = elsaMaterialName
                }
            }, true);
        }

        public void Unmap(int erpId, string elsaMaterialName, string eshopProductName)
        {
            _db.Sql().Call("unassignEshopItemMaterial")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@erpId", erpId)
                .WithParam("@erpProductName", eshopProductName)
                .WithParam("@materialName", elsaMaterialName)
                .NonQuery();
        }
    }
}
