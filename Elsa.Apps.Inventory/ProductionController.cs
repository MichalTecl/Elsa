using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.Production.Model;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("production")]
    public class ProductionController : ElsaControllerBase
    {
        private readonly IProductionFacade m_productionFacade;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialBatchFacade m_batchFacade;

        public ProductionController(
            IWebSession webSession,
            ILog log,
            IProductionFacade productionFacade,
            IMaterialRepository materialRepository,
            IUnitRepository unitRepository,
            IMaterialBatchFacade batchFacade)
            : base(webSession, log)
        {
            m_productionFacade = productionFacade;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_batchFacade = batchFacade;
        }

        public ProductionBatchModel GetProductionBatch(int batchId)
        {
            return m_productionFacade.GetProductionBatch(batchId);
        }

        public ProductionBatchModel CreateBatch(BatchSetupRequest rq)
        {
            var material = m_materialRepository.GetMaterialByName(rq.MaterialName);
            if (material == null)
            {
                throw new InvalidOperationException("Nedefinovaný materiál");
            }

            if (!material.IsManufactured)
            {
                throw new InvalidOperationException($"Materiál {rq.MaterialName} není nastaven jako vyráběný");
            }

            var unit = m_unitRepository.GetUnitBySymbol(rq.AmountUnitSymbol);
            if (unit == null)
            {
                throw new InvalidOperationException($"Neznama jednotka");
            }

            return m_productionFacade.CreateOrUpdateProductionBatch(rq.BatchId, material.Id, rq.BatchNumber, rq.Amount, unit);
        }

        public ProductionBatchModel AddComponentSourceBatch(
            int? materialBatchCompositionId,
            int productionBatchId,
            int materialId,
            string sourceBatchNumber,
            decimal usedAmount,
            string usedAmountUnitSymbol)
        {
            var batch = m_batchFacade.FindBatchBySearchQuery(materialId, sourceBatchNumber);
            if (batch == null)
            {
                throw new InvalidOperationException("Šarže nenalezena");
            }

            return m_productionFacade.SetComponentSourceBatch(
                materialBatchCompositionId,
                productionBatchId,
                batch.Id,
                usedAmount,
                usedAmountUnitSymbol);
        }

        public ProductionBatchModel RemoveComponentSourceBatch(int productionBatchId, int sourceBatchId)
        {
            return m_productionFacade.RemoveComponentSourceBatch(productionBatchId, sourceBatchId);
        }

        public IEnumerable<ProducedBatchModel> GetBatches(long? lastSeen)
        {
            foreach (var b in m_productionFacade.LoadProductionBatches(lastSeen, 10))
            {
                yield return
                    new ProducedBatchModel()
                        {
                            BatchId = b.Id,
                            Amount = new Amount(b.Volume, b.Unit).ToString(),
                            Author = b.Author.EMail,
                            BatchNr = b.BatchNumber,
                            DisplayDt = DateUtil.FormatDateWithAgo(b.Created),
                            MaterialName = b.Material.Name,
                            PagingDt = b.Created.Ticks,
                            Status = BatchStatus.Get(b)
                        };
            }
        }

        public ProductionBatchModel LockBatch(int batchId)
        {
            m_batchFacade.SetBatchLock(batchId, true, $"Locked from UI by {WebSession.User.EMail} {DateTime.Now}");

            return GetProductionBatch(batchId);
        }

        public ProductionBatchModel UnlockBatch(int batchId)
        {
            m_batchFacade.SetBatchLock(batchId, false, null);

            return GetProductionBatch(batchId);
        }
    }
}
