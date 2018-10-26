﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("warehouseActions")]
    public class WarehouseActionsController : ElsaControllerBase
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialBatchRepository m_batchRepository;
        

        public WarehouseActionsController(IWebSession webSession, ILog log, IMaterialRepository materialRepository, IUnitRepository unitRepository, IMaterialBatchRepository batchRepository)
            : base(webSession, log)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_batchRepository = batchRepository;
        }

        public IEnumerable<MaterialBatchViewModel> GetBottomMaterialBatches(long? before)
        {
            var to = before == null ? DateTime.Now.AddDays(1) : new DateTime(before.Value);
            var from = to.AddDays(-31);

            var batches = new List<MaterialBatchViewModel>();

            do
            {
                var result = m_batchRepository.GetMaterialBatches(from, to, true, null).ToList();
                batches.AddRange(result.Select(m => new MaterialBatchViewModel(m.Batch)));

                if (batches.Any(i => i.SortDt < to.Ticks))
                {
                    break;
                }

                to = to.AddDays(-1);
                from = from.AddDays(-31);
            }
            while ((to - from).TotalDays < 365);

            return batches;
        }
    }
}
