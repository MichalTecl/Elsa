using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Robowire.RobOrm.Core.DefaultRules;
using Robowire.RobOrm.Core.Internal;

namespace Robowire.RobOrm.Core.Query.Model
{
    public class SlowDataModelHelper : IDataModelHelper
    {
        private readonly IEntityNamingConvention m_convention = new DefaultEntityNamingConvention();

        public IEnumerable<PropertyInfo> GetTableDataColumns(Type entityType)
        {
            foreach (var property in ReflectionUtil.GetAllProperties(entityType))
            {
                if (m_convention.IsColumn(property))
                {
                    yield return property;
                }
            }
        }

        public IEnumerable<ReferenceInfo> GetReferences(Type entityType)
        {
            foreach (var property in ReflectionUtil.GetAllProperties(entityType))
            {
                var refType = m_convention.TryGetRefEntityType(property);
                if (refType == null)
                {
                    continue;
                }

                string leftKeyColumnName;
                string rightKeyColumnName;

                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    leftKeyColumnName = LocalKeyAttribute.GetLocalKeyName(
                        property,
                        m_convention.GetPrimaryKeyProperty(entityType).Name);
                    rightKeyColumnName = ForeignKeyAttribute.GetForeignKeyName(
                        property,
                        $"{GetTableName(entityType)}Id");
                }
                else
                {
                    leftKeyColumnName = LocalKeyAttribute.GetLocalKeyName(property, $"{property.Name}Id");
                    rightKeyColumnName = ForeignKeyAttribute.GetForeignKeyName(
                        property,
                        m_convention.GetPrimaryKeyProperty(refType).Name);
                }

                yield return
                    new ReferenceInfo(property.Name, entityType, leftKeyColumnName, null, refType, rightKeyColumnName);
            }
        }

        public string GetTableName(Type entityType)
        {
            return NamingHelper.GetEntityName(entityType);
        }

        public string GetColumnName(PropertyInfo columnProperty)
        {
            return columnProperty.Name;
        }

        public bool IsFkDefinedByanotherProperty(Type entityType, PropertyInfo columnProperty)
        {
            var props = ReflectionUtil.GetAllProperties(entityType);

            foreach (var p in props)
            {
                if (p.Name == ForeignKeyAttribute.GetForeignKeyName(p, null))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
