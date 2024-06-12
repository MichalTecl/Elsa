using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common
{
    public class DataIndex<TIndex, TValue>
    {
        private readonly Dictionary<TIndex, TValue> _data;

        public DataIndex(IEnumerable<TValue> data, Func<TValue, TIndex> idSelector)
        {
            _data = data.ToDictionary(idSelector);
        }

        public TValue Get(TIndex id, TValue def)
        {
            if(_data.TryGetValue(id, out var result))
            {
                return result;
            }

            return def;
        }

        public TProp Get<TProp>(TIndex id, Func<TValue, TProp> propSelector, TProp def)
        {
            if (_data.TryGetValue(id, out var result))
            {
                return propSelector(result);
            }

            return def;
        }
    }
}
