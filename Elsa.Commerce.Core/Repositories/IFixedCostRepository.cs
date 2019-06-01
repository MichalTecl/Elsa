using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Commerce.Core.Repositories
{
    public interface IFixedCostRepository
    {
        IEnumerable<IFixedCostType> GetFixedCostTypes();

        IFixedCostType SetFixedCostType(int? id, string name, int percent);

        void DeleteFixedCostType(int id);

        IEnumerable<IFixedCostValue> GetValues(int year, int month);

        void SetValue(int typeId, int year, int month, decimal value);
    }
}
