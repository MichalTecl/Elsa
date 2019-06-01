using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Repositories.Automation;
using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Commerce.Core.Repositories
{
    public class FixedCostRepository : IFixedCostRepository
    {
        private readonly IRepository<IFixedCostType> m_costTypeRepository;

        public FixedCostRepository(IRepositoryFactory repositories)
        {
            m_costTypeRepository = repositories.GetForSmallTable<IFixedCostType>();
        }

        public IEnumerable<IFixedCostType> GetFixedCostTypes()
        {
            return m_costTypeRepository.All().OrderBy(e => e.Name);
        }

        public IFixedCostType SetFixedCostType(int? id, string name, int percent)
        {
            if (percent < 0 || percent > 100)
            {
                throw new InvalidOperationException($"Procento musi byt cele cislo 0 az 100");
            }

            return m_costTypeRepository.Set(id,
                e =>
                {
                    e.Name = name;
                    e.PercentToDistributeAmongProducts = percent;
                });
        }

        public void DeleteFixedCostType(int id)
        {
            m_costTypeRepository.Delete(id);
        }
    }
}
