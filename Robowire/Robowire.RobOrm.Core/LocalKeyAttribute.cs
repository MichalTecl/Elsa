using System;
using System.Reflection;

namespace Robowire.RobOrm.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LocalKeyAttribute : Attribute
    {
        public LocalKeyAttribute(string localKeyColumnName)
        {
            LocalKeyColumnName = localKeyColumnName;
        }

        public string LocalKeyColumnName { get; private set; }

        public static string GetLocalKeyName(PropertyInfo property, string prefferedKeyName)
        {
            return (GetCustomAttribute(property, typeof(LocalKeyAttribute)) as LocalKeyAttribute)?.LocalKeyColumnName ?? prefferedKeyName;
        }
    }
}
