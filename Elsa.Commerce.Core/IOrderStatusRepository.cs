using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrderStatusRepository : IOrderStatusTranslator
    {
        IEnumerable<IOrderStatus> GetAllStatuses();

        IOrderStatus GetStatus(int statusId);
    }
}
