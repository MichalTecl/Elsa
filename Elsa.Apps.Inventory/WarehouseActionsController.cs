﻿using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("warehouseActions")]
    public class WarehouseActionsController : ElsaControllerBase
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly ISupplierRepository m_supplierRepository;

        public WarehouseActionsController(IWebSession webSession, ILog log, IMaterialRepository materialRepository, IUnitRepository unitRepository, IMaterialBatchRepository batchRepository, IMaterialBatchFacade batchFacade, ISupplierRepository supplierRepository)
            : base(webSession, log)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_batchRepository = batchRepository;
            m_batchFacade = batchFacade;
            m_supplierRepository = supplierRepository;
        }

        public IEnumerable<MaterialBatchViewModel> GetBottomMaterialBatches(long? before)
        {
            var to = before == null ? DateTime.Now.AddDays(1) : new DateTime(before.Value);
            var from = to.AddDays(-31);

            var batches = new List<MaterialBatchViewModel>();

            do
            {
                var result = m_batchRepository.GetMaterialBatches(from, to, true, null).ToList();
                
                batches.AddRange(result.Select(m => new MaterialBatchViewModel(m.Batch, m_supplierRepository)));

                if (batches.Any(i => i.SortDt < to.Ticks))
                {
                    break;
                }

                to = to.AddDays(-1);
                from = from.AddDays(-31);
            }
            while ((to - from).TotalDays < 365);

            return batches.OrderByDescending(b => b.SortDt);
        }

        public MaterialBatchViewModel SaveBottomMaterialBatch(MaterialBatchViewModel model)
        {
            EnsureUserRight(InventoryUserRights.MaterialBatchEdits);

            var material = m_materialRepository.GetMaterialByName(model.MaterialName);
            if (material == null)
            {
                throw new InvalidOperationException($"Neznámý název materiálu '{model.MaterialName}'");
            }

            var unit = m_unitRepository.GetUnitBySymbol(model.UnitName);
            if (unit == null)
            {
                throw new InvalidOperationException($"Neznámý název měrné jednotky '{model.UnitName}'");
            }
            
            var received = "AUTO".Equals(model.DisplayDt, StringComparison.InvariantCultureIgnoreCase) ? DateTime.Now : StringUtil.ParseDateTime(model.DisplayDt);

            var result = m_batchRepository.SaveBottomLevelMaterialBatch(
                model.Id ?? 0,
                material.Adaptee,
                model.Volume,
                unit,
                model.BatchNumber,
                received,
                model.Price ?? 0m,
                model.InvoiceNumber,
                model.SupplierName,
                model.CurrencySymbol,
                model.VariableSymbol
                );

            return new MaterialBatchViewModel(result.Batch, m_supplierRepository);
        }

        public void DeleteMaterialBatch(int batchId)
        {
            EnsureUserRight(InventoryUserRights.MaterialBatchEdits);

            m_batchFacade.DeleteBatch(batchId);
        }
    }
}
