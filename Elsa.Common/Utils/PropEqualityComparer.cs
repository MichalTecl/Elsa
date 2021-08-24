using System;
using System.Collections.Generic;

namespace Elsa.Common.Utils
{
    public class PropEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, object> m_propAccessor;

        public PropEqualityComparer(Func<T, object> propAccessor)
        {
            m_propAccessor = propAccessor;
        }

        public bool Equals(T x, T y)
        {
            return object.Equals(m_propAccessor(x), m_propAccessor(y));
        }

        public int GetHashCode(T obj)
        {
            return m_propAccessor(obj).GetHashCode();
        }
    }
}
