using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.StockEvents
{
    public interface IStockEventRepository
    {
        IEnumerable<IStockEventType> GetAllEventTypes();

        void SaveEvent(int eventTypeId, int materialId, string batchNumber, decimal quantity, string reason, string unitSymbol, long? sourceOrderId = null);

        IEnumerable<IMaterialStockEvent> GetBatchEvents(BatchKey key);

        IEnumerable<IMaterialStockEvent> GetEvents(DateTime @from, DateTime to, int inventoryId);

        void DeleteStockEvent(int eventId);

        void MoveOrderToEvent(long returnedOrderId, int eventTypeId, string reason);

        IEnumerable<IMaterialStockEvent> GetEvents(DateTime from, DateTime to, long sourcePurchaseOrderId);
    }
}
