using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Commerce;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class OrderStatusRepository : IOrderStatusRepository
    {
        private readonly IOrderStatusTranslator _translator;
        private readonly IDatabase _database;

        private List<IOrderStatus> _statuses;

        public OrderStatusRepository(IOrderStatusTranslator translator, IDatabase database)
        {
            _translator = translator;
            _database = database;
        }

        public string Translate(int statusId)
        {
            return _translator.Translate(statusId);
        }

        public string Translate(IOrderStatus status)
        {
            return Translate(status.Id);
        }

        public IEnumerable<IOrderStatus> GetAllStatuses()
        {
            return _statuses ?? (_statuses = _database.SelectFrom<IOrderStatus>().Execute().ToList());
        }

        public IOrderStatus GetStatus(int statusId)
        {
            return _statuses.FirstOrDefault(s => s.Id == statusId);
        }
    }
}
