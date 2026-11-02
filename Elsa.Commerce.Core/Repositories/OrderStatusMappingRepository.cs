using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Integration;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class OrderStatusMappingRepository : IOrderStatusMappingRepository
    {
        private readonly IDatabase _database;
        
        private readonly Dictionary<int, Dictionary<string, IErpOrderStatusMapping>> _cachedMappings = new Dictionary<int, Dictionary<string, IErpOrderStatusMapping>>();

        public OrderStatusMappingRepository(IDatabase database)
        {
            _database = database;
        }

        public IDictionary<string, IErpOrderStatusMapping> GetMappings(int erpId)
        {
            Dictionary<string, IErpOrderStatusMapping> mappings;
            if (!_cachedMappings.TryGetValue(erpId, out mappings))
            {
                mappings =
                    _database.SelectFrom<IErpOrderStatusMapping>()
                        .Where(m => m.ErpId == erpId)
                        .Execute()
                        .ToDictionary(m => m.ErpStatusId, m => m);

                _cachedMappings.Add(erpId, mappings);
            }

            return mappings;
        }
    }
}
