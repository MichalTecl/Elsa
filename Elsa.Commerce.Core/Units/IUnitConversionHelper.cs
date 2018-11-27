using System.Collections.Generic;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.Units
{
    public interface IUnitConversionHelper : IUnitConvertor
    {
        IMaterialUnit GetPrefferedUnit(IMaterialUnit a, IMaterialUnit b);

        bool AreCompatible(int unitId1, int unitId2);

        IEnumerable<IMaterialUnit> GetCompatibleUnits(int sourceUnitId);
        
        Amount ConvertAmount(Amount sourceAmount, int targetUnitId);
    }
}
