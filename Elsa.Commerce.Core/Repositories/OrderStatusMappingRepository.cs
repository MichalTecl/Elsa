using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class OrderStatusMappingRepository : IOrderStatusMappingRepository
    {
        private readonly IDatabase m_database;
        
        private readonly Dictionary<int, Dictionary<string, IErpOrderStatusMapping>> m_cachedMappings = new Dictionary<int, Dictionary<string, IErpOrderStatusMapping>>();

        public OrderStatusMappingRepository(IDatabase database)
        {
            m_database = database;
        }

        public IDictionary<string, IErpOrderStatusMapping> GetMappings(int erpId)
        {
            Dictionary<string, IErpOrderStatusMapping> mappings;
            if (!m_cachedMappings.TryGetValue(erpId, out mappings))
            {
                mappings =
                    m_database.SelectFrom<IErpOrderStatusMapping>()
                        .Where(m => m.ErpId == erpId)
                        .Execute()
                        .ToDictionary(m => m.ErpStatusId, m => m);

                m_cachedMappings.Add(erpId, mappings);
            }

            return mappings;
        }
    }
}
