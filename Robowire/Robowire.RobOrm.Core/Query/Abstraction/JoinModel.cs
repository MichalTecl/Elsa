namespace Robowire.RobOrm.Core.Query.Model
{
    public class JoinModel
    {
        public JoinType JoinType { get; set; }

        public string JoinedTableAlias { get; set; }

        public string JoinedTableName { get; set; }

        public string Condition { get; set; }

        public override string ToString()
        {
            return $"{JoinType} JOIN [{JoinedTableName}] as [{JoinedTableAlias}] ON ({Condition})";
        }
    }
}
