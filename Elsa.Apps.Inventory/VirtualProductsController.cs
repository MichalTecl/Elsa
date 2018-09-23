using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;

using Newtonsoft.Json;

using Robowire.RoboApi;

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
        
        public VirtualProductsController(IWebSession webSession, ILog log, IVirtualProductRepository virtualProductRepository, IErpRepository erpRepository, ICache cache, IMaterialRepository materialRepository, IVirtualProductFacade virtualProductFacade)
            : base(webSession, log)
        {
            m_virtualProductRepository = virtualProductRepository;
            m_erpRepository = erpRepository;
            m_cache = cache;
            m_materialRepository = materialRepository;
            m_virtualProductFacade = virtualProductFacade;
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

        public IEnumerable<MaterialInfo> GetAllMaterials()
        {
            return m_materialRepository.GetAllMaterials().Select(m => new MaterialInfo(m));
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
