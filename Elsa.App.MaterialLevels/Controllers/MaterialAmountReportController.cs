using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.MaterialLevels.Components;
using Elsa.App.MaterialLevels.Components.Model;
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
        private readonly IMaterialLevelsLoader m_levelsLoader;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IInventoryWatchRepository m_inventoryWatchRepository;
        private readonly IMaterialThresholdRepository m_materialThresholdRepository;
        private readonly IUnitRepository m_unitRepository;
        
        public MaterialAmountReportController(IWebSession webSession, ILog log, IMaterialLevelsLoader levelsLoader,
            IMaterialRepository materialRepository, IInventoryWatchRepository inventoryWatchRepository, IMaterialThresholdRepository materialThresholdRepository, IUnitRepository unitRepository) : base(webSession, log)
        {
            m_levelsLoader = levelsLoader;
            m_materialRepository = materialRepository;
            m_inventoryWatchRepository = inventoryWatchRepository;
            m_materialThresholdRepository = materialThresholdRepository;
            m_unitRepository = unitRepository;
        }

        public IEnumerable<MaterialLevelEntryModel> GetLevels(int inventoryId)
        {
            return m_levelsLoader.Load(inventoryId);
        }

        public IEnumerable<InventoryModel> GetInventories(bool quick)
        {
            return m_levelsLoader.GetInventories();
        }

        public IEnumerable<InventoryModel> GetUnwatchedInventories()
        {
            return m_inventoryWatchRepository.GetUnwatchedInventories().Select(i => new InventoryModel(null)
            {
                Id = i.Id,
                Name = i.Name
            });
        }

        public IEnumerable<InventoryModel> WatchInventory(int inventoryId)
        {
            m_inventoryWatchRepository.WatchInventory(inventoryId);

            return GetInventories(false);
        }

        public IEnumerable<InventoryModel> UnwatchInventory(int inventoryId)
        {
            m_inventoryWatchRepository.UnwatchInventory(inventoryId);
            return GetInventories(false);
        }

        public void SetThreshold(int materialId, string thresholdText)
        {
            if (string.IsNullOrEmpty(thresholdText))
            {
                m_materialThresholdRepository.DeleteThreshold(materialId);
                return;
            }

            var thresholdEntry = MaterialEntry.Parse(thresholdText, true);

            var thresholdUnit = m_unitRepository.GetUnitBySymbol(thresholdEntry.UnitName);
            if (thresholdUnit == null)
            {
                throw new InvalidOperationException($"Neznámý symbol jednotky \"{thresholdEntry.UnitName}\"");
            }

            m_materialThresholdRepository.SaveThreshold(materialId,
                thresholdEntry.Amount,
                thresholdUnit.Id);
        }
    }
}
