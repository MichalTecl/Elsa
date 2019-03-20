using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder.Internal
{
    internal static class ReflectionHelper
    {
        public static PropertyInfo GetPropertyInfo<T, TProperty>(Expression<Func<T, TProperty>> propertyLambda)
        {
            var type = typeof(T);

            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");
            }

            if ((type != propInfo.ReflectedType) && !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a property that is not from type {type}.");
            }

            return propInfo;
        }
    }
}
