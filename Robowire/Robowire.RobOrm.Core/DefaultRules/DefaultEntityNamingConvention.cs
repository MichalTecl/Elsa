using System;
using System.Collections;
using System.Linq;
using System.Reflection;

using Robowire.RobOrm.Core.Internal;

namespace Robowire.RobOrm.Core.DefaultRules
{
    public class DefaultEntityNamingConvention : IEntityNamingConvention
    {
        public string GetColumnName(PropertyInfo property)
        {
            try
            {
                if (!IsColumn(property))
                {
                    return null;
                }

                return property.Name;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to map property column {property.DeclaringType}.{property.Name}", ex);
            }
        }

        public bool IsColumn(PropertyInfo property)
        {
            var propType = property.PropertyType;

            var nType = Nullable.GetUnderlyingType(propType);
            if (nType != null)
            {
                propType = nType;
            }

            if (propType.IsInterface || propType.IsAbstract)
            {
                return false;
            }

            if (property.GetCustomAttributes().OfType<IDbTypeAttribute>().Any())
            {
                return true;
            }

            return SqlTypeMapper.GetSqlTypeMappingExists(propType);
        }

        public Type TryGetRefEntityType(PropertyInfo property)
        {
            if (!property.CanRead)
            {
                return null;
            }

            var pType = property.PropertyType;

            if (pType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pType))
            {
                pType = pType.GetGenericArguments()[0];
            }

            var result = Attribute.IsDefined(pType, typeof(EntityAttribute)) ? pType : null;

            if ((result != null) && property.CanWrite)
            {
                throw new InvalidOperationException($"Invalid definition of {property.DeclaringType?.Name}.{property.Name}: Entity relation property must be read-only.");
            }

            return result;
        }

        public PropertyInfo GetPrimaryKeyProperty(Type entityType)
        {
            var entityAttribute = Attribute.GetCustomAttribute(entityType, typeof(EntityAttribute)) as EntityAttribute;
            var propertyName = (entityAttribute?.PrimaryKeyProperty) ?? "Id";

            return ReflectionUtil.GetProperty(entityType, propertyName);
        }
    }
}
