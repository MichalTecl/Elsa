using System;
using System.Reflection;

namespace Robowire.RobOrm.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        public ForeignKeyAttribute(string foreignKeyColumnName)
        {
            ForeignKeyColumnName = foreignKeyColumnName;
        }

        public string ForeignKeyColumnName { get; private set; }

        public static string GetForeignKeyName(PropertyInfo property, string prefferedKeyName)
        {
            return (GetCustomAttribute(property, typeof(ForeignKeyAttribute)) as ForeignKeyAttribute)?.ForeignKeyColumnName ?? prefferedKeyName;
        }
    }
}
