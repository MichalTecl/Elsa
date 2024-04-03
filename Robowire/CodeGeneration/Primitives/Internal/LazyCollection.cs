using System;
using System.Collections.Generic;

namespace CodeGeneration.Primitives.Internal
{
    internal class LazyCollection<T> : List<T> where T : INamedReference
    {
        private readonly Func<T, T, bool> m_comparer;

        public LazyCollection(Func<T, T, bool> comparer)
        {
            m_comparer = comparer;
        }

        public LazyCollection()
            : this((arg1, arg2) => arg1.Name == arg2.Name)
        {
        }

        public T TryAdd(T newItem)
        {
            bool dummy;
            return TryAdd(newItem, out dummy);
        }

        public T TryAdd(T newItem, out bool alreadyExisted)
        {
            foreach (var item in this)
            {
                if (m_comparer(newItem, item))
                {
                    alreadyExisted = true;
                    return item;
                }
            }

            Add(newItem);
            alreadyExisted = false;
            return newItem;
        }
    }

    internal class LazyCollection : LazyCollection<INamedReference>
    {
        public LazyCollection()
            : base((a,b) => a.Name == b.Name)
        {
        }
    }
}
