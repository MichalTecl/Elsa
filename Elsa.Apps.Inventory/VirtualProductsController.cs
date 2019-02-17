using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Core.Entities.Commerce.Inventory;

using Newtonsoft.Json;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Apps.Inventory
{
    [Controller("virtualProducts")]
    public class VirtualProductsController : ElsaControllerBase
    {
        private const string c_mappablesCacheKey = "AllMappableItemsConvertedToModelFor_ProjectId:{0}";

        private readonly IVirtualProductRepository m_virtualProductRepository;
        private readonly IErpRepository m_erpRepository;
        private readonly ICache m_cache;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IVirtualProductFacade m_virtualProductFacade;
        private readonly IMaterialFacade m_materialFacade;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IDatabase m_database;
        private readonly IMaterialThresholdRepository m_materialThresholdRepository;

        public VirtualProductsController(IWebSession webSession, ILog log, IVirtualProductRepository virtualProductRepository, IErpRepository erpRepository, ICache cache, IMaterialRepository materialRepository, IVirtualProductFacade virtualProductFacade, IMaterialFacade materialFacade, IUnitConversionHelper conversionHelper, IDatabase database, IMaterialThresholdRepository materialThresholdRepository)
            : base(webSession, log)
        {
            m_virtualProductRepository = virtualProductRepository;
            m_erpRepository = erpRepository;
            m_cache = cache;
            m_materialRepository = materialRepository;
            m_virtualProductFacade = virtualProductFacade;
            m_materialFacade = materialFacade;
            m_conversionHelper = conversionHelper;
            m_database = database;
            m_materialThresholdRepository = materialThresholdRepository;
        }

        public IEnumerable<VirtualProductViewModel> GetVirtualProducts(string searchQuery)
        {
            var normQuery = StringUtil.NormalizeSearchText(99, searchQuery);

            var allProds = m_virtualProductRepository.GetAllVirtualProducts();

            foreach (var prod in allProds)
            {
                if (!string.IsNullOrWhiteSpace(normQuery))
                {
                    var normName = StringUtil.NormalizeSearchText(200, prod.Name);
                    if (!normName.Contains(normQuery))
                    {
                        continue;
                    }
                }

                yield return MapVirtualProductToVm(prod);
            }
        }

        private VirtualProductViewModel MapVirtualProductToVm(Core.Entities.Commerce.Inventory.IVirtualProduct prod)
        {
            var materials = m_materialRepository.GetMaterialsByVirtualProductId(prod.Id);

            var result = new VirtualProductViewModel(prod);
            result.MaterialEntries.AddRange(materials.Select(m => new MaterialCompositionInfo(m.Material)
            {
                UnitId = m.Unit.Id,
                UnitSymbol = m.Unit.Symbol,
                Amount = m.Amount,
                CompositionId = m.CompositionId ?? -1
            }));
            return result;
        }

        public IEnumerable<MappableItemViewModel> GetMappableItems(string searchQuery)
        {
            var allMappables = GetAllMappablesThroughCache();

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return allMappables.OrderBy(i => i.ViewText);
            }

            var queryParts =
                searchQuery.Split(' ').Select(i => StringUtil.NormalizeSearchText(10, new[] { i })).ToArray();

            return allMappables.Where(i => queryParts.All(keyword => i.SearchText.Contains(keyword))).OrderBy(i => i.ViewText);
        }

        public IEnumerable<MappableItemViewModel> GetItemsMappedToVirtualProduct(int virtualProductId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MappableItemViewModel> MapOrderItemToProduct(int virtualProductId, MappableItemViewModel.MappableItemId mappableItemId, string activeSearchQuery)
        {
            try
            {
                var orderItemId = JsonConvert.SerializeObject(mappableItemId);

                var mappable = GetAllMappablesThroughCache().FirstOrDefault(m => m.Id == orderItemId);
                if (mappable == null)
                {
                    throw new InvalidOperationException("Id not found");
                }

                m_virtualProductRepository.Map(mappable.ErpId, mappable.ErpProductId, mappable.OrderItemText, virtualProductId);
            }
            finally
            {
                m_cache.Remove(GetMappablesCacheKey());
            }

            return GetMappableItems(activeSearchQuery);
        }

        public IEnumerable<MappableItemViewModel> RemoveVirtualProductMapping(int virtualProductId, MappableItemViewModel.MappableItemId mappableItemId, string activeSearchQuery)
        {
            try
            {
                var orderItemId = JsonConvert.SerializeObject(mappableItemId);

                var mappable = GetAllMappablesThroughCache().FirstOrDefault(m => m.Id == orderItemId);
                if (mappable == null)
                {
                    throw new InvalidOperationException("Id not found");
                }

                m_virtualProductRepository.Unmap(mappable.ErpId, mappable.ErpProductId, mappable.OrderItemText, virtualProductId);
            }
            finally
            {
                m_cache.Remove(GetMappablesCacheKey());
            }

            return GetMappableItems(activeSearchQuery);
        }

        public IEnumerable<MaterialInfo> GetAllMaterials(int? inventoryId)
        {
            return m_materialRepository.GetAllMaterials(inventoryId).Select(m => new MaterialInfo(m));
        }

        public VirtualProductViewModel GetVirtualProductById(int id)
        {
            var vp = m_virtualProductRepository.GetVirtualProductById(id);
            if (vp == null)
            {
                return null;
            }

            return MapVirtualProductToVm(vp);
        }

        public VirtualProductViewModel SaveVirtualProduct(VirtualProductEditRequestModel request)
        {
            try
            {
                var vp = m_virtualProductFacade.ProcessVirtualProductEditRequest(request.VirtualProductId, request.UnhashedName, request.Materials.Select(m => m.DisplayText).ToArray());

                return MapVirtualProductToVm(vp);
            }
            finally
            {
                m_cache.Remove(GetMappablesCacheKey());
            }
        }

        public void DeleteVirtualProduct(int vpId)
        {
            m_virtualProductRepository.DeleteVirtualProduct(vpId);
            m_materialRepository.CleanCache();
        }

        public IEnumerable<IExtendedMaterialModel> SearchMaterials(string query, int? inventoryId)
        {
            var allMats = m_materialRepository.GetAllMaterials(inventoryId);

            var normQuery = StringUtil.NormalizeSearchText(99, query);

            if (string.IsNullOrWhiteSpace(normQuery))
            {
                return allMats;
            }

            return allMats.Where(m => StringUtil.NormalizeSearchText(99, m.Name).Contains(normQuery));
        }

        public IExtendedMaterialModel GetMaterialById(int id)
        {
            var material = m_materialRepository.GetMaterialById(id);

            if (material == null)
            {
                return null;
            }
            
            return material;
        }

        public IExtendedMaterialModel SaveMaterial(MaterialEditRequestModel request)
        {
            var thresholdText = request.HasThreshold ? (request.ThresholdText ?? string.Empty) : null;

            using (var tx = m_database.OpenTransaction())
            {
                var saved = m_materialFacade.ProcessMaterialEditRequest(
                    request.MaterialId,
                    request.MaterialName,
                    request.NominalAmountText,
                    request.MaterialInventoryId,
                    request.AutomaticBatches,
                    request.RequiresPrice,
                    request.RequiresInvoice,
                    request.Materials.Select(s => s.DisplayText),
                    thresholdText);
                
                if (saved.ProductionSteps.Any() || request.ProductionSteps.Any())
                {
                    saved = m_materialFacade.ProcessProductionStepsEditRequest(saved, request.ProductionSteps);
                }

                m_cache.Remove(GetMappablesCacheKey());

                tx.Commit();

                return saved;
            }
        }

        public IEnumerable<string> GetAllMaterialNames()
        {
            return m_materialRepository.GetAllMaterials(null).Select(m => m.Name);
        }

        public IEnumerable<IMaterialInventory> GetMaterialInventories()
        {
            return m_materialRepository.GetMaterialInventories();
        }

        public Dictionary<string, List<string>> GetAllMaterialsWithCompatibleUnits()
        {
            var materials = m_materialRepository.GetAllMaterials(null).ToList();

            var result = new Dictionary<string, List<string>>(materials.Count);
            foreach (var m in materials)
            {
                var lst = m_conversionHelper.GetCompatibleUnits(m.Adaptee.NominalUnitId).Select(u => u.Symbol).ToList();
                result[m.Name] = lst;
            }

            return result;
        }

        public void DeleteMaterial(int id)
        {
            m_materialRepository.DeleteMaterial(id);

            m_cache.Remove(GetMappablesCacheKey());
            m_materialRepository.CleanCache();
            m_virtualProductRepository.CleanCache();
        }

        private List<MappableItemViewModel> GetAllMappablesThroughCache()
        {
            var cacheKey = GetMappablesCacheKey();

            return m_cache.ReadThrough(cacheKey, TimeSpan.FromMinutes(10), LoadMappables);
        }

        private List<MappableItemViewModel> LoadMappables()
        {
            var allItems = m_virtualProductRepository.GetMappableItems().ToList();

            var result = new List<MappableItemViewModel>(allItems.Count);

            foreach (var item in allItems)
            {
                IErp erp = null;
                if (item.ErpId != null)
                {
                    erp = m_erpRepository.GetErp(item.ErpId.Value);
                }

                var model = MappableItemViewModel.Create(item, erp);
                result.Add(model);

                foreach (var virtualProduct in m_virtualProductRepository.GetVirtualProducts(item.ErpId, item.ErpProductId, item.ItemName))
                {
                    if (model.AssignedVirtualProducts.All(existing => existing.VirtualProductId != virtualProduct.Id))
                    {
                        var vpvm = new VirtualProductViewModel(virtualProduct);

                        var virtualProductComponents = m_materialRepository.GetMaterialsByVirtualProductId(vpvm.VirtualProductId).ToList();

                        var materialsSb = new StringBuilder();

                        foreach (var materialComponent in virtualProductComponents)
                        {
                            materialComponent.Material.Print(materialsSb, " ");
                        }

                        vpvm.MaterialsText = materialsSb.ToString();
                        model.AssignedVirtualProducts.Add(vpvm);
                    }
                }
            }

            return result;
        }

        private string GetMappablesCacheKey()
        {
            return string.Format(c_mappablesCacheKey, WebSession.Project.Id);
        }
    }
}