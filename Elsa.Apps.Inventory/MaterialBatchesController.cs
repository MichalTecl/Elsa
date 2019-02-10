using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
