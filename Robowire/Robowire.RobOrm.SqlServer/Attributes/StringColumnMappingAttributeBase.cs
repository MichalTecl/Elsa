using System;
using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Attributes
{
    public class NVarchar : Attribute, IDbTypeAttribute
    {
        public const int Max = 0;

        public NVarchar(int length, bool nullable)
        {
            IsNullable = nullable;

            var strLen = length > 0 ? length.ToString() : "max";
            ColumnDeclarationTypeText = $"nvarchar({strLen})";
        }

        public string ColumnDeclarationTypeText { get; }

        public bool IsNullable { get; }
    }
}
