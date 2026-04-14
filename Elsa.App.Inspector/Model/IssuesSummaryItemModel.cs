namespace Elsa.App.Inspector.Model
{
    public class IssuesSummaryItemModel
    {
        public int TypeId { get; set; }

        public string TypeName { get; set; }

        public int IssuesCount { get; set; }

        public bool ShowAssignments { get; set; }

        public int AssignedUsersCount { get; set; }
    }
}
