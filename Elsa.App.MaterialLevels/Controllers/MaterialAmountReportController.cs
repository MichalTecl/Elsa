using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.MaterialLevels.Components;
using Elsa.App.MaterialLevels.Components.Model;
using Elsa.Apps.Inventory;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RoboApi;

namespace Elsa.App.MaterialLevels.Controllers
{
    [Controller("MaterialAmountReport")]
    public class MaterialAmountReportController : ElsaControllerBase
    {
        private readonly IMaterialLevelsLoader _levelsLoader;
        private readonly IMaterialRepository _materialRepository;
        private readonly IInventoryWatchRepository _inventoryWatchRepository;
        private readonly IMaterialThresholdRepository _materialThresholdRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly MaterialOrderingRepository _orderingRepository;

        public MaterialAmountReportController(IWebSession webSession, ILog log, IMaterialLevelsLoader levelsLoader,
            IMaterialRepository materialRepository, IInventoryWatchRepository inventoryWatchRepository, IMaterialThresholdRepository materialThresholdRepository, IUnitRepository unitRepository, MaterialOrderingRepository orderingRepository) : base(webSession, log)
        {
            _levelsLoader = levelsLoader;
            _materialRepository = materialRepository;
            _inventoryWatchRepository = inventoryWatchRepository;
            _materialThresholdRepository = materialThresholdRepository;
            _unitRepository = unitRepository;
            _orderingRepository = orderingRepository;
        }

        public IEnumerable<MaterialLevelEntryModel> GetLevels(int inventoryId)
        {
            if (!HasUserRight(InventoryUserRights.MaterialLevels))
                return new List<MaterialLevelEntryModel>(0);

            return _levelsLoader.Load(inventoryId);
        }

        public IEnumerable<InventoryModel> GetInventories(bool quick)
        {
            if (!HasUserRight(InventoryUserRights.MaterialLevels))
                return new List<InventoryModel>(0);

            return _levelsLoader.GetInventories();
        }

        public IEnumerable<InventoryModel> GetUnwatchedInventories()
        {
            if (!HasUserRight(InventoryUserRights.MaterialLevels))
                return new List<InventoryModel>(0);

            return _inventoryWatchRepository.GetUnwatchedInventories().Select(i => new InventoryModel(null)
            {
                Id = i.Id,
                Name = i.Name
            });
        }

        public IEnumerable<InventoryModel> WatchInventory(int inventoryId)
        {
            EnsureUserRight(InventoryUserRights.MaterialLevels);

            _inventoryWatchRepository.WatchInventory(inventoryId);

            return GetInventories(false);
        }

        public IEnumerable<InventoryModel> UnwatchInventory(int inventoryId)
        {
            EnsureUserRight(InventoryUserRights.MaterialLevels);

            _inventoryWatchRepository.UnwatchInventory(inventoryId);
            return GetInventories(false);
        }

        public void SetThreshold(int materialId, string thresholdText)
        {
            EnsureUserRight(InventoryUserRights.MaterialLevelsChangeThresholds);

            if (string.IsNullOrEmpty(thresholdText))
            {
                _materialThresholdRepository.DeleteThreshold(materialId);
                return;
            }

            var thresholdEntry = MaterialEntry.Parse(thresholdText, true);

            var thresholdUnit = _unitRepository.GetUnitBySymbol(thresholdEntry.UnitName);
            if (thresholdUnit == null)
            {
                throw new InvalidOperationException($"Neznámý symbol jednotky \"{thresholdEntry.UnitName}\"");
            }

            _materialThresholdRepository.SaveThreshold(materialId,
                thresholdEntry.Amount,
                thresholdUnit.Id);
        }

        public void SetComment(int materialId, string text)
        {
            _materialRepository.SaveMaterialComment(materialId, text, InventoryUserRights.MaterialCommentsEdit);
        }

        public string SetOrderDt(int materialId, string value)
        {            
            EnsureUserRight(InventoryUserRights.MaterialLevelsChangeThresholds);

            DateTime? parsed = null;

            if (!string.IsNullOrEmpty(value)) 
            {
                parsed = DateTime.ParseExact(value, "yyyy-MM-dd", null);
            }

            _materialRepository.SaveOrderDt(materialId, parsed);

            return parsed?.ToString(MaterialLevelEntryModel.OrderDtViewFormat) ?? string.Empty;
        }

        public void SetDeadline(int materialId, int days)
        {
            EnsureUserRight(InventoryUserRights.MaterialLevelsChangeOrderDt);

            _orderingRepository.SetOrderDeliveryDeadline(materialId, DateTime.Now.AddDays(days).Date);
        }

        public void DeleteDeadline(int materialId)
        {
            EnsureUserRight(InventoryUserRights.MaterialLevelsChangeOrderDt);

            _orderingRepository.SetOrderDeliveryDeadline(materialId, null);
        }
    }
}
