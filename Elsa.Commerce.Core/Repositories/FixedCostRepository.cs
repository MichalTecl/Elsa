using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Repositories.Automation;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Accounting;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class FixedCostRepository : IFixedCostRepository
    {
        private readonly IRepository<IFixedCostType> m_costTypeRepository;
        private readonly IRepository<IFixedCostValue> m_costValueRepository;
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public FixedCostRepository(IRepositoryFactory repositories, IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
            var hStart = DateTime.Now.Year - 2;

            m_costTypeRepository = repositories.GetForSmallTable<IFixedCostType>();
            m_costValueRepository =
                repositories.GetForSmallTable<IFixedCostValue>(q => q.Where(e => e.Year >= hStart));
        }

        public IEnumerable<IFixedCostType> GetFixedCostTypes()
        {
            return m_costTypeRepository.All().OrderBy(e => e.Name);
        }

        public IFixedCostType SetFixedCostType(int? id, string name, int percent)
        {
            if ((percent < 0) || (percent > 100))
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

        public IEnumerable<IFixedCostValue> GetValues(int year, int month)
        {
            return m_costValueRepository.All().Where(e => (e.Year == year) && (e.Month == month));
        }

        public void SetValue(int typeId, int year, int month, decimal value)
        {
            var ett = m_costValueRepository.All()
                .FirstOrDefault(e => (e.FixedCostTypeId == typeId) && (e.Year == year) && (e.Month == month));

            m_costValueRepository.Set(ett?.Id,
                e =>
                {
                    e.FixedCostTypeId = typeId;
                    e.Month = month;
                    e.Year = year;
                    e.Value = value;
                });
        }

        public void CalculateFixedCostComponents(int year, int month)
        {
            var rqDate = new DateTime(year, month, 1).AddMonths(1).AddSeconds(-1);

            if (rqDate >= DateTime.Now)
            {
                throw new InvalidOperationException($"Není možné zpracovat nepřímé náklady před ukončením {month}/{year}");
            }

            m_database.Sql().Call("ApplyFixedCosts")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@userId", m_session.User.Id)
                .WithParam("@year", year)
                .WithParam("@month", month)
                .WithParam("@overwrite", false).NonQuery();
        }
    }
}
