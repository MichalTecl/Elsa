using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class InvoiceFormsCollection<T>
    {
        public string Title { get; set; }

        public int? Month { get; set; }

        public int? Year { get; set; }

        public int InvoiceFormTypeId { get; set; }

        public string TotalPriceFormatted { get; set; }
        
        public List<T> Forms { get; } = new List<T>();
    }
}
