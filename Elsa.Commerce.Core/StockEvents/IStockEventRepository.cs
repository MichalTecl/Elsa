using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.StockEvents
{
    public interface IStockEventRepository
    {
        IEnumerable<IStockEventType> GetAllEventTypes();
    }
}
