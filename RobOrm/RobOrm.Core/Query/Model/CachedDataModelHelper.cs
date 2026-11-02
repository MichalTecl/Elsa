using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RobOrm.Core.Query.Model
{
    public class CachedDataModelHelper : IDataModelHelper
    {
        private readonly ConcurrentDictionary<Type, List<PropertyInfo>> _dataColumns = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private readonly ConcurrentDictionary<Type, List<ReferenceInfo>> _references = new ConcurrentDictionary<Type, List<ReferenceInfo>>();
        private readonly ConcurrentDictionary<Type, string> _entityNames = new ConcurrentDictionary<Type, string>();

        private readonly IDataModelHelper _source = new SlowDataModelHelper();

        public IEnumerable<PropertyInfo> GetTableDataColumns(Type entityType)
        {
            return _dataColumns.GetOrAdd(entityType, t => _source.GetTableDataColumns(t).ToList());
        }

        public IEnumerable<ReferenceInfo> GetReferences(Type entityType)
        {
            return _references.GetOrAdd(entityType, t => _source.GetReferences(t).ToList());
        }

        public string GetTableName(Type entityType)
        {
            return _entityNames.GetOrAdd(entityType, t => _source.GetTableName(t));
        }

        public string GetColumnName(PropertyInfo columnProperty)
        {
            return _source.GetColumnName(columnProperty);
        }

        public bool IsFkDefinedByanotherProperty(Type entityType, PropertyInfo columnProperty)
        {
            return _source.IsFkDefinedByanotherProperty(entityType, columnProperty);
        }
    }
}
