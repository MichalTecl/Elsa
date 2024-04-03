using System;

namespace Robowire.RobOrm.Core.Internal
{
    internal static class NamingHelper
    {
        public static string GetDefaultEntityName(Type entityType)
        {
            var entityName = entityType.Name;
            if (entityType.IsInterface
                && entityName.StartsWith("I")
                && (entityName.Length > 2)
                && char.IsUpper(entityName[1]))
            {
                entityName = entityName.Substring(1);
            }

            return entityName;
        }

        public static string GetEntityName(Type entityType)
        {
            var entityAttribute = Attribute.GetCustomAttribute(entityType, typeof(EntityAttribute)) as EntityAttribute;
            if (entityAttribute == null)
            {
                return null;
            }

            return entityAttribute.EntityName ?? GetDefaultEntityName(entityType);
        }
    }
}
