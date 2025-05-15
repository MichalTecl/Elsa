namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public class ColumnInfo
    {
        public ColumnInfo(int displayOrder, string id, string title)
        {
            DisplayOrder = displayOrder;
            Id = id;
            Title = title;
        }

        public int DisplayOrder { get; }
        public string Id { get; }
        public string Title { get; }
    }
}
