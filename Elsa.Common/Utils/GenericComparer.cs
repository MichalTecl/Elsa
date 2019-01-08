using System;
using System.Collections.Generic;

namespace Elsa.Common.Utils
{
    public class GenericComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> m_comparison;

        public GenericComparer(Func<T, T, int> comparison)
        {
            m_comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return m_comparison(x, y);
        }
    }
}
