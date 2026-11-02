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
using Elsa.Common.EntityComments;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Core.Entities.Commerce.Inventory;

using Newtonsoft.Json;

using Robowire.RoboApi;
using RobOrm.Core;

namespace Elsa.Apps.Inventory
{
    [Controller("virtualProducts")]
    public class VirtualProductsController : ElsaControllerBase
    {
        private const string c_mappablesCacheKey = "AllMappableItemsConvertedToModelFor_ProjectId:{0}";

        private readonly IVirtualProductRepository _virtualProductRepository;
        private readonly IErpRepository _erpRepository;
        private readonly ICache _cache;
        private readonly IMaterialRepository _materialRepository;
        private readonly IVirtualProductFacade _virtualProductFacade;
        private readonly IMaterialFacade _materialFacade;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IDatabase _database;
        private readonly IMaterialThresholdRepository _materialThresholdRepository;
        private readonly IEntityCommentsFacade _entityCommentsFacade;

        public VirtualProductsController(IWebSession webSession, ILog log, IVirtualProductRepository virtualProductRepository, IErpRepository erpRepository, ICache cache, IMaterialRepository materialRepository, IVirtualProductFacade virtualProductFacade, IMaterialFacade materialFacade, IUnitConversionHelper conversionHelper, IDatabase database, IMaterialThresholdRepository materialThresholdRepository, IEntityCommentsFacade entityCommentsFacade)
            : base(webSession, log)
        {
            _virtualProductRepository = virtualProductRepository;
            _erpRepository = erpRepository;
            _cache = cache;
            _materialRepository = materialRepository;
            _virtualProductFacade = virtualProductFacade;
            _materialFacade = materialFacade;
            _conversionHelper = conversionHelper;
            _database = database;
            _materialThresholdRepository = materialThresholdRepository;
            _entityCommentsFacade = entityCommentsFacade;
        }

        public IEnumerable<VirtualProductViewModel> GetVirtualProducts(string searchQuery)
        {
            var normQuery = StringUtil.NormalizeSearchText(99, searchQuery);

            var allProds = _virtualProductRepository.GetAllVirtualProducts();

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
            var materials = _materialRepository.GetMaterialsByVirtualProductId(prod.Id);

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
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

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
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

            try
            {
                var orderItemId = JsonConvert.SerializeObject(mappableItemId);

                var mappable = GetAllMappablesThroughCache().FirstOrDefault(m => m.Id == orderItemId);
                if (mappable == null)
                {
                    throw new InvalidOperationException("Id not found");
                }

                _virtualProductRepository.Map(mappable.ErpId, mappable.ErpProductId, mappable.OrderItemText, virtualProductId);
            }
            finally
            {
                _cache.Remove(GetMappablesCacheKey());
            }

            return GetMappableItems(activeSearchQuery);
        }

        public IEnumerable<MappableItemViewModel> RemoveVirtualProductMapping(int virtualProductId, MappableItemViewModel.MappableItemId mappableItemId, string activeSearchQuery)
        {
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

            try
            {
                var orderItemId = JsonConvert.SerializeObject(mappableItemId);

                var mappable = GetAllMappablesThroughCache().FirstOrDefault(m => m.Id == orderItemId);
                if (mappable == null)
                {
                    throw new InvalidOperationException("Id not found");
                }

                _virtualProductRepository.Unmap(mappable.ErpId, mappable.ErpProductId, mappable.OrderItemText, virtualProductId);
            }
            finally
            {
                _cache.Remove(GetMappablesCacheKey());
            }

            return GetMappableItems(activeSearchQuery);
        }

        public IEnumerable<MaterialInfo> GetAllMaterials(int? inventoryId)
        {
            return _materialRepository.GetAllMaterials(inventoryId, true).Select(m => new MaterialInfo(m));
        }

        public VirtualProductViewModel GetVirtualProductById(int id)
        {
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

            var vp = _virtualProductRepository.GetVirtualProductById(id);
            if (vp == null)
            {
                return null;
            }

            return MapVirtualProductToVm(vp);
        }

        public VirtualProductViewModel SaveVirtualProduct(VirtualProductEditRequestModel request)
        {
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

            try
            {
                var vp = _virtualProductFacade.ProcessVirtualProductEditRequest(request.VirtualProductId, request.UnhashedName, request.Materials.Select(m => m.DisplayText).ToArray());

                return MapVirtualProductToVm(vp);
            }
            finally
            {
                _cache.Remove(GetMappablesCacheKey());
            }
        }

        public void DeleteVirtualProduct(int vpId)
        {
            EnsureUserRight(InventoryUserRights.ProductsAndTags);

            _virtualProductRepository.DeleteVirtualProduct(vpId);
            _materialRepository.CleanCache();
        }

        public IEnumerable<IExtendedMaterialModel> SearchMaterials(string query, int? inventoryId)
        {
            var allMats = _materialRepository.GetAllMaterials(inventoryId, true);

            var normQuery = StringUtil.NormalizeSearchText(99, query);

            if (string.IsNullOrWhiteSpace(normQuery))
            {
                return allMats;
            }

            return allMats.Where(m => StringUtil.NormalizeSearchText(99, m.Name).Contains(normQuery));
        }

        public IExtendedMaterialModel GetMaterialById(int id)
        {
            var material = _materialRepository.GetMaterialById(id);

            if (material == null)
            {
                return null;
            }

            _entityCommentsFacade.TryLoadComment(InventoryUserRights.MaterialCommentsView, material);
            
            return material;
        }

        public IExtendedMaterialModel SaveMaterial(MaterialEditRequestModel request)
        {
            EnsureUserRight(InventoryUserRights.MaterialEdits);

            var thresholdText = request.HasThreshold ? (request.ThresholdText ?? string.Empty) : null;

            using (var tx = _database.OpenTransaction())
            {                
                var saved = _materialFacade.ProcessMaterialEditRequest(request);

                saved.CommentText = request.Comment;

                _entityCommentsFacade.AddComment(InventoryUserRights.MaterialCommentsEdit, saved);

                _cache.Remove(GetMappablesCacheKey());

                tx.Commit();

                return saved;
            }
        }

        public IExtendedMaterialModel HideMaterial(int id)
        {
            EnsureUserRight(InventoryUserRights.MaterialEdits);
            
            return _materialRepository.SetMaterialHidden(id, true);
        }

        public IExtendedMaterialModel UnhideMaterial(int id)
        {
            EnsureUserRight(InventoryUserRights.MaterialEdits);

            return _materialRepository.SetMaterialHidden(id, false);
        }

        public IEnumerable<string> GetAllMaterialNames()
        {
            return _materialRepository.GetAllMaterials(null, true).Select(m => m.Name);
        }

        public IEnumerable<IMaterialInventory> GetMaterialInventories()
        {
            return _materialRepository.GetMaterialInventories();
        }

        public Dictionary<string, List<string>> GetAllMaterialsWithCompatibleUnits()
        {
            var materials = _materialRepository.GetAllMaterials(null, false).ToList();

            var result = new Dictionary<string, List<string>>(materials.Count);
            foreach (var m in materials)
            {
                var lst = _conversionHelper.GetCompatibleUnits(m.Adaptee.NominalUnitId).Select(u => u.Symbol).ToList();
                result[m.Name] = lst;
            }

            return result;
        }
               
        public void DeleteMaterial(int id)
        {
            EnsureUserRight(InventoryUserRights.MaterialEdits);

            _materialRepository.DeleteMaterial(id);

            _cache.Remove(GetMappablesCacheKey());
            _materialRepository.CleanCache();
            _virtualProductRepository.CleanCache();
        }

        private List<MappableItemViewModel> GetAllMappablesThroughCache()
        {
            var cacheKey = GetMappablesCacheKey();

            return _cache.ReadThrough(cacheKey, TimeSpan.FromMinutes(10), LoadMappables);
        }

        private List<MappableItemViewModel> LoadMappables()
        {
            var allItems = _virtualProductRepository.GetMappableItems().ToList();

            var result = new List<MappableItemViewModel>(allItems.Count);

            foreach (var item in allItems)
            {
                IErp erp = null;
                if (item.ErpId != null)
                {
                    erp = _erpRepository.GetErp(item.ErpId.Value);
                }

                var model = MappableItemViewModel.Create(item, erp);
                result.Add(model);

                foreach (var virtualProduct in _virtualProductRepository.GetVirtualProducts(item.ErpId, item.ErpProductId, item.ItemName))
                {
                    if (model.AssignedVirtualProducts.All(existing => existing.VirtualProductId != virtualProduct.Id))
                    {
                        var vpvm = new VirtualProductViewModel(virtualProduct);

                        var virtualProductComponents = _materialRepository.GetMaterialsByVirtualProductId(vpvm.VirtualProductId).ToList();

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