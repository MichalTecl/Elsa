using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public readonly struct SalesTrendTick
    {
        public static readonly SalesTrendTick Empty = new SalesTrendTick(0, true);

        public readonly int Percent;
        public readonly bool IsEmpty;

        private SalesTrendTick(int percent, bool isEmpty)
        {
            Percent = percent;
            IsEmpty = isEmpty;
        }

        public SalesTrendTick(int percent) : this(percent, false) { }
    }

    internal class SalesTrend
    {
        private decimal _max = 0;
        private readonly decimal[] _values;

        public SalesTrend(int historyDepth)
        {
            _values = new decimal[historyDepth + 1];
        }

        public void Add(int month, decimal value)
        {
            if (value > _max)
                _max = value;

#if (DEBUG)
            if (_values[month] > 0)
                throw new InvalidOperationException("overlap");
#endif

            _values[month] = value;
        }

        public IEnumerable<SalesTrendTick> GetModel()
        {
            foreach(var i in _values)
            {
                if (i == 0)
                {
                    yield return SalesTrendTick.Empty;
                    continue;
                }

                var p = (int) Math.Round(i / _max * 100m, 0);
                yield return new SalesTrendTick(p);
            }
        }
    }

}
