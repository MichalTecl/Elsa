using System;
using System.Reflection;

namespace Robowire.RobOrm.Core
{
    public interface IEntityNamingConvention
    {
        string GetColumnName(PropertyInfo property);

        bool IsColumn(PropertyInfo property);

        Type TryGetRefEntityType(PropertyInfo property);

        PropertyInfo GetPrimaryKeyProperty(Type entityType);
    }
}
