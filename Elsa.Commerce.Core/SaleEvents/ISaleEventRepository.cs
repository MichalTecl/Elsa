using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;

namespace Elsa.Commerce.Core.SaleEvents
{
    public interface ISaleEventRepository
    {
        ISaleEvent GetEventById(int id);

        IEnumerable<ISaleEventAllocation> GetAllocationsByEventId(int saleEventId);

        IEnumerable<ISaleEvent> GetEvents(int pageNumber, int pageSize);

        ISaleEvent WriteEvent(int id, Action<ISaleEvent> entity, IEnumerable<SaleEventAllocationDto> allocations);
    }
}
