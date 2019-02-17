using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("stockEvents")]
    public class StockEventsController : ElsaControllerBase
    {
        private readonly IStockEventRepository m_eventRepository;
        private readonly IMaterialBatchFacade m_batchFacade;

        public StockEventsController(IWebSession webSession, ILog log, IStockEventRepository eventRepository, IMaterialBatchFacade batchFacade)
            : base(webSession, log)
        {
            m_eventRepository = eventRepository;
            m_batchFacade = batchFacade;
        }

        public IEnumerable<IStockEventType> GetEventTypes()
        {
            return m_eventRepository.GetAllEventTypes();
        }

        public IMaterialBatch FindBatch(int materialId, string query)
        {
            return m_batchFacade.FindBatchBySearchQuery(materialId, query);
        }

        public BatchEventAmountSuggestions GetSuggestedAmounts(int eventTypeId, int batchId)
        {
            return m_batchFacade.GetEventAmountSuggestions(eventTypeId, batchId);
        }
    }
}
