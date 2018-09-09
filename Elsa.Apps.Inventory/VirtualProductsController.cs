using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;

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
        
        public VirtualProductsController(IWebSession webSession, ILog log, IVirtualProductRepository virtualProductRepository, IErpRepository erpRepository, ICache cache)
            : base(webSession, log)
        {
            m_virtualProductRepository = virtualProductRepository;
            m_erpRepository = erpRepository;
            m_cache = cache;
        }

        public IEnumerable<VirtualProductViewModel> GetVirtualProducts(string searchQuery)
        {
            throw new NotImplementedException();
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

        public IEnumerable<MappableItemViewModel> MapOrderItemToProduct(int virtualProductId, string orderItemId, string activeSearchQuery)
        {
            try
            {
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

        public IEnumerable<MappableItemViewModel> RemoveVirtualProductMapping(int virtualProductId, string orderItemId, string activeSearchQuery)
        {
            try
            {
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
                        model.AssignedVirtualProducts.Add(new VirtualProductViewModel(virtualProduct));
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
