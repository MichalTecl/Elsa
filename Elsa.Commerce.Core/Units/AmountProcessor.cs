using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Utils;

namespace Elsa.Commerce.Core.Units
{
    public class AmountProcessor
    {
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IUnitRepository m_unitRepository;

        public AmountProcessor(IUnitConversionHelper conversionHelper, IUnitRepository unitRepository)
        {
            m_conversionHelper = conversionHelper;
            m_unitRepository = unitRepository;
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

        public Amount Min(Amount a, params Amount[] b)
        {
            foreach(var amt in b)
            {
                a = Min(a, amt);
            }

            return a;
        }

        public Amount Divide(Amount a, Amount b)
        {
            return Calculate(a, b, (c, d) => c/d);
        }

        public Amount Multiply(Amount a, Amount b)
        {
            return Calculate(a, b, (c, d) => c*d);
        }

        public Amount Sum(IEnumerable<Amount> a)
        {
            Amount result = null;

            foreach (var item in a)
            {
                result = Add(result, item);
            }

            return result;
        }

        public Amount ConvertToSuitableUnit(Amount source)
        {
            if (source.Unit == null)
            {
                return source;
            }
            
            var availableUnits = m_conversionHelper.GetCompatibleUnits(source.Unit.Id).ToList();
            if (!availableUnits.Any())
            {
                return source;
            }

            var bestUnit = source.Unit;
            var readability = StringUtil.GetReadability(source.Value);

            foreach (var availableUnit in availableUnits)
            {
                //var converted = m_conversionHelper.ConvertAmount(source.Unit.Id, availableUnit.Id, )
            }

            throw new NotImplementedException();
        }

        private Amount Calculate(Amount a, Amount b, Func<decimal, decimal, decimal> numericOp)
        {
            if ((a == null) && (b == null))
            {
                return null;
            }

            if (a == null)
            {
                a = new Amount(0, b.Unit);
            }

            if (b == null)
            {
                b = new Amount(0, a.Unit);
            }

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

        public Amount ToAmount(decimal amount, string unitSymbol)
        {
            return new Amount(amount, m_unitRepository.GetUnitBySymbol(unitSymbol));
        }

        public Amount ToAmount(decimal amount, int unitId)
        {
            return new Amount(amount, m_unitRepository.GetUnit(unitId));
        }

        public Amount LinearScale(Amount x1, Amount y1, Amount x2)
        {
            var factor = Divide(y1, x1);
            return Multiply(x2, factor);
        }

        public bool GreaterThan(Amount a, Amount b)
        {
            var sub = Subtract(a, b);
            return sub.IsPositive;
        }
    }
}
