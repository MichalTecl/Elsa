namespace Robowire.RobOrm.Core.Query.Model
{
    public sealed class SelectedColumnModel
    {
        public readonly string EntityAlias;

        public readonly string ColumnName;

        public readonly string DirectExpression;

        public string ColumnAlias => $"{EntityAlias}.{ColumnName}";

        public SelectedColumnModel(string entityAlias, string columnName, string directExpression)
        {
            EntityAlias = entityAlias;
            ColumnName = columnName;
            DirectExpression = directExpression;
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DirectExpression))
            {
                return $"{DirectExpression} as \"{DirectExpression.Replace("[", string.Empty).Replace("]", string.Empty)}\"";
            }

            return $"[{EntityAlias}].[{ColumnName}] as \"{ColumnAlias}\"";
        }
    }
}
