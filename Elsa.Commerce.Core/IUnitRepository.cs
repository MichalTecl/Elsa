using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core
{
    public interface IUnitRepository
    {
        IMaterialUnit GetUnit(int unitId);

        IMaterialUnit GetUnitBySymbol(string symbol);

        IEnumerable<IMaterialUnit> GetAllUnits();
    }
}
