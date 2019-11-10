using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("stockEvents")]
    public class StockEventsController : ElsaControllerBase
    {
        private readonly IStockEventRepository m_eventRepository;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly AmountProcessor m_amountProcessor;

        public StockEventsController(IWebSession webSession, ILog log, IStockEventRepository eventRepository,
            IMaterialBatchFacade batchFacade,
            IMaterialBatchRepository batchRepository, AmountProcessor amountProcessor)
            : base(webSession, log)
        {
            m_eventRepository = eventRepository;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
            m_amountProcessor = amountProcessor;
        }

        public IEnumerable<IStockEventType> GetEventTypes()
        {
            return m_eventRepository.GetAllEventTypes();
        }

        public IMaterialBatch FindBatch(int materialId, string query)
        {
            return m_batchRepository.GetBatches(m_batchFacade.FindBatchBySearchQuery(materialId, query)).Single();
        }

        public BatchEventAmountSuggestions GetSuggestedAmounts(int eventTypeId, int batchId)
        {
            return m_batchFacade.GetEventAmountSuggestions(eventTypeId, batchId);
        }

        public void SaveEvent(int eventTypeId, int materialId, string batchNumber, decimal quantity, string reason, string unitSymbol)
        {
            m_eventRepository.SaveEvent(eventTypeId, materialId, batchNumber, quantity, reason, unitSymbol);
        }

        public IEnumerable<StockEventViewModel> GetBatchEvents(string batchId, string eventTypeName)
        {
            var etype =
                m_eventRepository.GetAllEventTypes().FirstOrDefault(etp => etp.TabTitle == eventTypeName).Ensure();

            var rawEvents = 
                m_eventRepository.GetBatchEvents(BatchKey.Parse(batchId))
                    .Where(e => e.TypeId == etype.Id)
                    .Select(e => new StockEventViewModel(e));


            var result = new List<StockEventViewModel>();

            foreach (var rawEvent in rawEvents)
            {
                var sameGrpEvt = result.FirstOrDefault(e => e.GroupingKey.Equals(rawEvent.GroupingKey));
                if (sameGrpEvt != null)
                {
                    sameGrpEvt.Join(rawEvent, m_amountProcessor);
                    continue;
                }

                result.Add(rawEvent);
            }

            return result;
        }

        public void DeleteStockEvent(int eventId)
        {
            m_eventRepository.DeleteStockEvent(eventId, true);
        }
    }
}
