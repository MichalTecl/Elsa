using System;
using System.Reflection;

namespace Elsa.Common.Configuration
{
    public class ConfigEntryAttribute : Attribute
    {
        public ConfigEntryAttribute(string key, string defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
        }

        public ConfigEntryAttribute(string key) : this(key, null)
        {
        }
        
        public string Key { get; }

        public string DefaultValue { get; }

        public static Tuple<string, string> GetConfigKeyAndDefault(PropertyInfo property)
        {
            if (!property.CanWrite || !property.CanRead)
            {
                return null;
            }

            var atr = GetCustomAttribute(property, typeof(ConfigEntryAttribute)) as ConfigEntryAttribute;

            return new Tuple<string, string>(atr?.Key ?? property.Name, atr?.DefaultValue);
        }
    }
}
