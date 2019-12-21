using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Elsa.Common.Utils
{
    public static class ObjectExtensions
    {
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static T Ensure<T>(this T entity, string errorMessage = "Invalid entity reference") where T : class
        {
            if (entity == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return entity;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static T Ensure<T>(this Nullable<T> entity, string errorMessage = "Invalid entity reference") where T:struct
        {
            if (entity == null)
            {
                throw new ArgumentNullException(errorMessage);
            }

            return entity.Value;
        }

        public static void SetAndThrowIfReassign<TObj, TValue>(this TObj obj,
            Expression<Func<TObj, TValue>> exp,
            TValue value)
        {
            var property = GetPropertyFromExpression(exp);

            var currentValue = property.GetValue(obj);

            if ((currentValue != null) && !currentValue.Equals(default(TValue)))
            {
                if (currentValue.Equals(value))
                {
                    return;
                }

                throw new InvalidOperationException($"Cannot set value of {obj}.{property.Name} to \"{value}\" because there is already value \"{currentValue}\" set");
            }

            property.SetValue(obj, value);
        }
        
        public static PropertyInfo GetPropertyFromExpression<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a method, not a property.");

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a field, not a property.");

            if ((type != propInfo.ReflectedType) && !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(
                    $"Expression '{propertyLambda.ToString()}' refers to a property that is not from type {type}.");

            return propInfo;
        }

        public static void AddRange<T>(this HashSet<T> hs, IEnumerable<T> items)
        {
            foreach (var t in items)
            {
                hs.Add(t);
            }
        }
    }
}
