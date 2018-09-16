using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
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
    }
}
