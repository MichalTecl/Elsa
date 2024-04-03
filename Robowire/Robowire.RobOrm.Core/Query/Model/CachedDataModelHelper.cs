using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Robowire.RobOrm.Core.Query.Model
{
    public class CachedDataModelHelper : IDataModelHelper
    {
        private readonly ConcurrentDictionary<Type, List<PropertyInfo>> m_dataColumns = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private readonly ConcurrentDictionary<Type, List<ReferenceInfo>> m_references = new ConcurrentDictionary<Type, List<ReferenceInfo>>();
        private readonly ConcurrentDictionary<Type, string> m_entityNames = new ConcurrentDictionary<Type, string>();

        private readonly IDataModelHelper m_source = new SlowDataModelHelper();

        public IEnumerable<PropertyInfo> GetTableDataColumns(Type entityType)
        {
            return m_dataColumns.GetOrAdd(entityType, t => m_source.GetTableDataColumns(t).ToList());
        }

        public IEnumerable<ReferenceInfo> GetReferences(Type entityType)
        {
            return m_references.GetOrAdd(entityType, t => m_source.GetReferences(t).ToList());
        }

        public string GetTableName(Type entityType)
        {
            return m_entityNames.GetOrAdd(entityType, t => m_source.GetTableName(t));
        }

        public string GetColumnName(PropertyInfo columnProperty)
        {
            return m_source.GetColumnName(columnProperty);
        }

        public bool IsFkDefinedByanotherProperty(Type entityType, PropertyInfo columnProperty)
        {
            return m_source.IsFkDefinedByanotherProperty(entityType, columnProperty);
        }
    }
}
