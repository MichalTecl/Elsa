using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Robowire.RobOrm.Core.Internal
{
    public static class SqlTypeMapper
    {
        private static readonly Dictionary<Type, string> s_clrToSqlName = new Dictionary<Type, string>
        {
                { typeof(long), "bigint" },
                { typeof(byte[]), "binary" },
                { typeof(bool), "bit" },
                { typeof(DateTime), "datetime" },
                {
                    typeof(DateTimeOffset),
                    "datetimeoffset"
                },
                { typeof(double), "float" },
                { typeof(int), "int" },
                { typeof(decimal), "decimal(19,4)" },
                { typeof(float), "real" },
                { typeof(short), "smallint" },
                { typeof(TimeSpan), "time" },
                { typeof(byte), "tinyint" },
                {
                    typeof(Guid), "uniqueidentifier"
                },

            //{ typeof(Xml), "xml" }
        };

        public static string GetSqlTypeName(Type clrType, int length)
        {
            return GetSqlTypeName(clrType, length, true);
        }

        public static bool GetSqlTypeMappingExists(Type clrType)
        {
            return GetSqlTypeName(clrType, 0) != null;
        }

        public static IDbTypeAttribute GetColumnType(PropertyInfo property)
        {
            var atr = Attribute.GetCustomAttributes(property).OfType<IDbTypeAttribute>().FirstOrDefault();
            if (atr == null)
            {
                var nullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                var typeName = GetSqlTypeName(property.PropertyType, 0);

                atr = new DbType(typeName, nullable);
            }

            return atr;
        }

        private static string GetSqlTypeName(Type clrType, int length, bool throwIfNotFound)
        {
            clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;

            string sqlName;
            if (!s_clrToSqlName.TryGetValue(clrType, out sqlName))
            {
                if (throwIfNotFound)
                {
                    throw new InvalidOperationException($"There is no explicit conversion for type {clrType}");
                }
                else
                {
                    return null;
                }
            }

            return string.Format(sqlName, length);
        }

    }

    internal sealed class DbType : IDbTypeAttribute
    {
        public DbType(string columnDeclarationTypeText, bool isNullable)
        {
            ColumnDeclarationTypeText = columnDeclarationTypeText;
            IsNullable = isNullable;
        }

        public string ColumnDeclarationTypeText { get; }

        public bool IsNullable { get; }
    }
}
