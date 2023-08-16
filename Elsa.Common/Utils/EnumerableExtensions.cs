using System.Collections.Generic;

namespace Elsa.Common.Utils
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<List<T>> Chunk<T>(this IEnumerable<T> src, int chunkSize) 
        {
            List<T> lst = new List<T>(chunkSize); 

            foreach(var i in src) 
            {                
                lst.Add(i);
                if (lst.Count == chunkSize) 
                {
                    yield return lst;
                    lst = new List<T>(chunkSize);
                }
            }

            if (lst.Count > 0)
                yield return lst;
        }
    }
}
