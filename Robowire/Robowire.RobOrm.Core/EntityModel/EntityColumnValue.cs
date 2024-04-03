namespace Robowire.RobOrm.Core.EntityModel
{
    public sealed class EntityColumnValue
    {
        public readonly string ColumnName;
        public readonly bool IsPk;
        public readonly object Value;

        public EntityColumnValue(string columnName, bool isPk, object value)
        {
            ColumnName = columnName;
            IsPk = isPk;
            Value = value;
        }

        public override string ToString()
        {
            return $"{ColumnName}:{Value}";
        }
    }
}
