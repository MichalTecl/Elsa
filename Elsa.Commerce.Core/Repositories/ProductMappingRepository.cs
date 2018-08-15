using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class ProductMappingRepository : IProductMappingRepository
    {
        private readonly IDatabase m_database;

        private readonly Dictionary<int, Dictionary<string, IErpProductMapping>> m_cache = new Dictionary<int, Dictionary<string, IErpProductMapping>>();

        public ProductMappingRepository(IDatabase database)
        {
            m_database = database;
        }

        public IDictionary<string, IErpProductMapping> GetMappings(int erpId)
        {
            Dictionary<string, IErpProductMapping> mappings;
            if (!m_cache.TryGetValue(erpId, out mappings))
            {
                mappings =
                    m_database.SelectFrom<IErpProductMapping>()
                        .Where(m => m.ErpId == erpId)
                        .Execute()
                        .ToDictionary(m => m.ErpProductId, m => m);

                m_cache.Add(erpId, mappings);
            }

            return mappings;
        }
    }
}
