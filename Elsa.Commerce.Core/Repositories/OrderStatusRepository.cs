using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class OrderStatusRepository : IOrderStatusRepository
    {
        private readonly IOrderStatusTranslator m_translator;
        private readonly IDatabase m_database;

        private List<IOrderStatus> m_statuses;

        public OrderStatusRepository(IOrderStatusTranslator translator, IDatabase database)
        {
            m_translator = translator;
            m_database = database;
        }

        public string Translate(int statusId)
        {
            return m_translator.Translate(statusId);
        }

        public string Translate(IOrderStatus status)
        {
            return Translate(status.Id);
        }

        public IEnumerable<IOrderStatus> GetAllStatuses()
        {
            return m_statuses ?? (m_statuses = m_database.SelectFrom<IOrderStatus>().Execute().ToList());
        }

        public IOrderStatus GetStatus(int statusId)
        {
            return m_statuses.FirstOrDefault(s => s.Id == statusId);
        }
    }
}
