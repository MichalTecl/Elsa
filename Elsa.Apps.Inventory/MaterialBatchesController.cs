using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("materialBatches")]
    public class MaterialBatchesController : ElsaControllerBase
    {
        private readonly IMaterialBatchFacade m_batchFacade;

        public MaterialBatchesController(IWebSession webSession, ILog log, IMaterialBatchFacade batchFacade)
            : base(webSession, log)
        {
            m_batchFacade = batchFacade;
        }

        public void DeleteBatch(int id)
        {
            m_batchFacade.DeleteBatch(id);
        }

        public void ReleaseUnsentOrdersAllocations()
        {
            m_batchFacade.ReleaseUnsentOrdersAllocations();
        }

        public void CutOrderAllocation(string handle)
        {
            // "orderId|batchKey"
            var parts = handle.Split('|');
            var orderId = int.Parse(parts[0]);

            if (parts.Length == 2)
            {
                var key = BatchKey.Parse(parts[1]);
                m_batchFacade.CutOrderAllocation(orderId, key);
            }
            else if (parts.Length == 1)
            {
                m_batchFacade.CutOrderAllocation(orderId, null);
            }
        }
    }
}
