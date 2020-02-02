using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Commerce.Core.Warehouse.BatchReporting;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("batchReporting")]
    public class BatchReportingController : ElsaControllerBase
    {
        private readonly IBatchReportingFacade m_reportingFacade;

        public BatchReportingController(IWebSession webSession, ILog log, IBatchReportingFacade reportingFacade) : base(webSession, log)
        {
            m_reportingFacade = reportingFacade;
        }

        public BatchReportModel Get(BatchReportQuery query)
        {
            if (query == null)
            {
                query = new BatchReportQuery
                {
                    PageNumber = 0
                };
            }

            return m_reportingFacade.QueryBatches(query);
        }
    }
}
