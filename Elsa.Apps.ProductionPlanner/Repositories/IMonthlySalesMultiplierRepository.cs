using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.ProductionPlanner.Repositories
{
    public interface IMonthlySalesMultiplierRepository
    {
        IEnumerable<MonthSalesMultiplier> GetMultipliers();

        void SaveMultiplier(DateTime month, int percent, string note);
    }
}
