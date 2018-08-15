namespace Elsa.Apps.Common.ViewModels
{
    public class ReportColumnDefinition
    {
        private string m_title;

        public string ColumnId { get; set; }

        public string Title
        {
            get
            {
                return m_title ?? ColumnId;
            }

            set
            {
                m_title = value;
            }
        }

    }
}
