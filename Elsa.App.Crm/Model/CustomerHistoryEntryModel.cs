using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class CustomerHistoryEntryModel
    {
        public int CustomerId { get; set; }
        public DateTime EventDt { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string TitleCssClass { get; set; }
        public string IconCssClass { get; set; }
        public string Author { get; set; }
        public bool IsLatestEvent { get; set; }
        public string EventDtF { get; set; }
    }
}
