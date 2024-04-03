using System;
using System.Collections.Generic;
using System.Reflection;

namespace Robowire.RobOrm.Core.Query.Model
{
    public interface IDataModelHelper
    {
        IEnumerable<PropertyInfo> GetTableDataColumns(Type entityType);

        IEnumerable<ReferenceInfo> GetReferences(Type entityType);

        string GetTableName(Type entityType);

        string GetColumnName(PropertyInfo columnProperty);

        bool IsFkDefinedByanotherProperty(Type entityType, PropertyInfo columnProperty);
    }
}
