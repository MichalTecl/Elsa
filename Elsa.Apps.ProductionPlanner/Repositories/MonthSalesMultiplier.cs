using Elsa.Apps.ProductionPlanner.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionPlanner.Repositories
{
    public class MonthSalesMultiplier
    {
        public MonthSalesMultiplier(IEnumerable<IMonthlySalesMultiplier> salesMultipliers) 
        {
            History = salesMultipliers.OrderByDescending(sm => sm.InsertDt).ToList();
            var actual = History.FirstOrDefault();
            if (actual == null) 
                throw new ArgumentException("No multipliers");

            Month = actual.ForecastMonth;
            ActualMultiplier = actual.MultiplierPercent;
            OverrideUserId = actual.IsSystemGenerated ? null : (int?)actual.InsertUserId;
        }

        public DateTime Month { get; set; }
        public int ActualMultiplier { get; set; }

        public int? OverrideUserId { get; set; }

        public List<IMonthlySalesMultiplier> History { get; set; }
    }
}
