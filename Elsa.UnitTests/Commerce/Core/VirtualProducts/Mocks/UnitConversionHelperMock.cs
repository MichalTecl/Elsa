using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts.Mocks
{
    internal class UnitConversionHelperMock : IUnitConversionHelper
    {
        public decimal ConvertAmount(int sourceUnitId, int targetUnitId, decimal sourceAmount)
        {
            if (sourceUnitId != targetUnitId)
            {
                throw new InvalidOperationException("Mock requires the same units");
            }

            return sourceAmount;
        }

        public IMaterialUnit GetPrefferedUnit(IMaterialUnit a, IMaterialUnit b)
        {
            return a;
        }

        public IMaterialUnit GetSmallestCompatibleUnit(IMaterialUnit u)
        {
            throw new NotImplementedException();
        }

        public bool AreCompatible(int unitId1, int unitId2)
        {
            return unitId1 == unitId2;
        }

        public IEnumerable<IMaterialUnit> GetCompatibleUnits(int sourceUnitId)
        {
            throw new NotImplementedException();
        }

        public Amount ConvertAmount(Amount sourceAmount, int targetUnitId)
        {
            throw new NotImplementedException();
        }
    }
}
