using System;
using System.Collections.Generic;

namespace Robowire.RobOrm.Core
{
    public static class CollectionExtensions
    {
        public static T Each<T>(this IEnumerable<T> collection)
        {
            throw new InvalidOperationException("The Each extension metod is intended only to be used in lambda Expression");
        }
    }
}
