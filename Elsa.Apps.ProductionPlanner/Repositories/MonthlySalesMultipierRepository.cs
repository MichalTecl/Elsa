using Elsa.Apps.ProductionPlanner.Entities;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionPlanner.Repositories
{
    public class MonthlySalesMultipierRepository : IMonthlySalesMultiplierRepository
    {
        private readonly IDatabase _db;
        private readonly ICache _cache;
        private readonly ISession _session;

        public MonthlySalesMultipierRepository(IDatabase db, ICache cache, ISession session)
        {
            _db = db;
            _cache = cache;
            _session = session;
        }

        public IEnumerable<MonthSalesMultiplier> GetMultipliers()
        {
            return _cache.ReadThrough(GetCacheKey(), TimeSpan.FromHours(1), () => {

                var startDt = DateTime.Now.AddMonths(-3);

                var lst = _db.SelectFrom<IMonthlySalesMultiplier>()
                .Where(m => m.ProjectId == _session.Project.Id)
                .Where(m => m.ForecastMonth >= startDt)
                .Execute()
                .GroupBy(m => m.ForecastMonth);

                var result = new List<MonthSalesMultiplier>(12);

                foreach(var g in lst) 
                {
                    result.Add(new MonthSalesMultiplier(g));
                }

                return result;
            });
        }

        public void SaveMultiplier(DateTime month, int percent, string note)
        {
            var mul = _db.New<IMonthlySalesMultiplier>();
            mul.ProjectId = _session.Project.Id;
            mul.InsertDt = DateTime.Now;
            mul.InsertUserId = _session.User.Id;
            mul.MultiplierPercent = percent;
            mul.Note = note;
            mul.ForecastMonth = month;

            _db.Save(mul);

            _cache.Remove(GetCacheKey());
        }

        private string GetCacheKey() 
        {
            return $"MonthlySalesMultipliers_{_session.Project.Id}";
        }
    }
}
