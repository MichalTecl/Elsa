using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace RobOrm.Core.EntityGeneration
{
    /// <summary>
    /// Used by generated entity setters. Compares values with the semantics of the C# == operator
    /// (user defined operators, lifted nullable comparison, reference equality for classes),
    /// exactly like the former generated C# code did.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EntityPropertyEquality<T>
    {
        private static readonly Func<T, T, bool> s_comparer = CreateComparer();

        public static bool AreEqual(T a, T b)
        {
            return s_comparer(a, b);
        }

        private static Func<T, T, bool> CreateComparer()
        {
            var a = Expression.Parameter(typeof(T), "a");
            var b = Expression.Parameter(typeof(T), "b");

            BinaryExpression equal;
            try
            {
                equal = Expression.Equal(a, b);
            }
            catch (InvalidOperationException)
            {
                // A type without == operator (C# would not compile the former generated code for it)
                var comparer = EqualityComparer<T>.Default;
                return comparer.Equals;
            }

            return Expression.Lambda<Func<T, T, bool>>(equal, a, b).Compile();
        }
    }
}
