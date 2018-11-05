using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;

namespace Elsa.Commerce.Core.Units
{
    public class AmountProcessor
    {
        private readonly IUnitConversionHelper m_conversionHelper;

        public AmountProcessor(IUnitConversionHelper conversionHelper)
        {
            m_conversionHelper = conversionHelper;
        }

        public Amount Subtract(Amount a, Amount b)
        {
            return Calculate(a, b, (x,y) => x - y);
        }

        public Amount Add(Amount a, Amount b)
        {
            return Calculate(a, b, (x, y) => x + y);
        }

        public Amount Min(Amount a, Amount b)
        {
            return Calculate(a, b, Math.Min);
        }

        private Amount Calculate(Amount a, Amount b, Func<decimal, decimal, decimal> numericOp)
        {
            if (a.Unit.Id == b.Unit.Id)
            {
                return new Amount(numericOp(a.Value, b.Value), a.Unit);
            }

            var targetUnit = m_conversionHelper.GetPrefferedUnit(a.Unit, b.Unit);

            var convertedA = m_conversionHelper.ConvertAmount(a.Unit.Id, targetUnit.Id, a.Value);
            var convertedB = m_conversionHelper.ConvertAmount(b.Unit.Id, targetUnit.Id, b.Value);

            var result = numericOp(convertedA, convertedB);

            return new Amount(result, targetUnit);
        }
    }
}
