using System;
using System.Reflection;

namespace Elsa.Common.Configuration
{
    public class ConfigEntryAttribute : Attribute, IConfigEntryDefinition
    {
        public ConfigEntryAttribute(string key, string defaultValue, params ConfigEntryScope[] scope)
        {
            Key = key;
            DefaultValueJson = defaultValue;
            Scope = scope;
        }

        public ConfigEntryAttribute(string key, params ConfigEntryScope[] scope) : this(key, null, scope) {}

        public ConfigEntryAttribute(params ConfigEntryScope[] scope) : this(null, scope) { }

        public ConfigEntryAttribute() : this(ConfigEntryScope.Project, ConfigEntryScope.Global) { }
        
        public string Key { get; }

        public string DefaultValueJson
        {
            get;
        }

        public ConfigEntryScope[] Scope
        {
            get;
        }

        public static IConfigEntryDefinition GetDefinition(PropertyInfo property)
        {
            var atr = property.GetCustomAttribute(typeof(ConfigEntryAttribute)) as IConfigEntryDefinition;
            if (atr == null)
            {
                return null;
            }

            if (!property.CanRead || !property.CanWrite)
            {
                throw new InvalidOperationException($"Invalid ConfigEntry - property must allow Get and Set");
            }

            return new ConfigEntryAttribute(atr.Key ?? property.Name, atr.DefaultValueJson, atr.Scope);
        }
    }
}
