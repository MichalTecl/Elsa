using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Repositories.Automation;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Accounting;
using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class FixedCostRepository : IFixedCostRepository
    {
        private readonly IRepository<IFixedCostType> _costTypeRepository;
        private readonly IRepository<IFixedCostValue> _costValueRepository;
        private readonly IDatabase _database;
        private readonly ISession _session;

        public FixedCostRepository(IRepositoryFactory repositories, IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
            var hStart = DateTime.Now.Year - 2;

            _costTypeRepository = repositories.GetForSmallTable<IFixedCostType>();
            _costValueRepository =
                repositories.GetForSmallTable<IFixedCostValue>(q => q.Where(e => e.Year >= hStart));
        }

        public IEnumerable<IFixedCostType> GetFixedCostTypes()
        {
            return _costTypeRepository.All().OrderBy(e => e.Name);
        }

        public IFixedCostType SetFixedCostType(int? id, string name, int percent)
        {
            if ((percent < 0) || (percent > 100))
            {
                throw new InvalidOperationException($"Procento musi byt cele cislo 0 az 100");
            }

            return _costTypeRepository.Set(id,
                e =>
                {
                    e.Name = name;
                    e.PercentToDistributeAmongProducts = percent;
                });
        }

        public void DeleteFixedCostType(int id)
        {
            _costTypeRepository.Delete(id);
        }

        public IEnumerable<IFixedCostValue> GetValues(int year, int month)
        {
            return _costValueRepository.All().Where(e => (e.Year == year) && (e.Month == month));
        }

        public void SetValue(int typeId, int year, int month, decimal value)
        {
            var ett = _costValueRepository.All()
                .FirstOrDefault(e => (e.FixedCostTypeId == typeId) && (e.Year == year) && (e.Month == month));

            _costValueRepository.Set(ett?.Id,
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

            _database.Sql().Call("ApplyFixedCosts")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@userId", _session.User.Id)
                .WithParam("@year", year)
                .WithParam("@month", month)
                .WithParam("@overwrite", false).NonQuery();
        }
    }
}
