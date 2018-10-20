using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("warehouseActions")]
    public class WarehouseActionsController : ElsaControllerBase
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IWarehouseRepository m_warehouseRepository;
        

        public WarehouseActionsController(IWebSession webSession, ILog log, IMaterialRepository materialRepository, IUnitRepository unitRepository, IWarehouseRepository warehouseRepository)
            : base(webSession, log)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_warehouseRepository = warehouseRepository;
        }

        public void SaveWarehouseFillEvent(WarehouseFillRequest request)
        {
            var unit = m_unitRepository.GetUnitBySymbol(request.UnitName);
            if (unit == null)
            {
                throw new InvalidOperationException($"Neznama jednotka '{request.UnitName}'");
            }

            var material = m_materialRepository.GetMaterialByName(request.MaterialName);
            if (material == null)
            {
                throw new InvalidOperationException($"Neznamy material '{request.MaterialName}'");
            }

            m_warehouseRepository.AddMaterialStockEvent(material.Adaptee, request.Amount, unit, request.Note);
        }
    }
}
