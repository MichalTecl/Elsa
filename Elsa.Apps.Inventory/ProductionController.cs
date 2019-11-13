using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IMaterialBatchRepository m_batchRepository;

        public ProductionController(
            IWebSession webSession,
            ILog log,
            IProductionFacade productionFacade,
            IMaterialRepository materialRepository,
            IUnitRepository unitRepository,
            IMaterialBatchFacade batchFacade, IMaterialBatchRepository batchRepository)
            : base(webSession, log)
        {
            m_productionFacade = productionFacade;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
        }

        public ProductionBatchModel GetProductionBatch(int batchId)
        {
            return m_productionFacade.GetProductionBatch(batchId);
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
