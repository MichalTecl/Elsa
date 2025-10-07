using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Crm.Model
{
    public class CustomerChanges
    {
        public int CustomerId { get; set; }
        public DateTime Day { get; set; }        
        public string Author { get; set; }
        public string Title => $"{Day:dd.MM.yyyy} - {Author}";

        public List<Change> Changes { get; } = new List<Change>();

        public void AddChange(string field, string oldValue, string newValue) 
        {
            Changes.Add(new Change 
            { 
                Index = Changes.Count + 1,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
            });
        }

        public class Change 
        {             
            public string Field { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
            public int Index { get; internal set; }
        }
    }
}
