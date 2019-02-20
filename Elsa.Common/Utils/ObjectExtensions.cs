using System;

namespace Elsa.Common.Utils
{
    public static class ObjectExtensions
    {
        public static T Ensure<T>(this T entity) where T : class
        {
            if (entity == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            return entity;
        }
    }
}
