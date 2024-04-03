using System;
using System.Collections.Generic;

using Robowire.RobOrm.Core.DefaultMethodMappers;

namespace Robowire.RobOrm.Core
{
    public static class QueryExtensions
    {
        [MethodMapper(typeof(LikeMethodMapper))]
        public static bool Like(this string value, string pattern)
        {
            throw new InvalidOperationException($"Do not call directly, for querying only");
        }

        [MethodMapper(typeof(InCsvMethodMapper))]
        public static bool InCsv<T>(this T value, IEnumerable<T> values)
        {
            throw new InvalidOperationException($"Do not call directly, for querying only");
        }

        [MethodMapper(typeof(InSubqueryMethodMapper))]
        public static bool InSubquery<T>(this T value, ITransformedQuery<T> values)
        {
            throw new InvalidOperationException($"Do not call directly, for querying only");
        }

    }
}
