using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class InvoiceFormsCollection<T>
    {
        public int? Id { get; set; }

        public string Title { get; set; }

        public int? Month { get; set; }

        public int? Year { get; set; }

        public int InvoiceFormTypeId { get; set; }

        public string TotalPriceFormatted { get; set; }
        
        public List<T> Forms { get; } = new List<T>();

        public List<GenerationInfoModel> Log { get; } = new List<GenerationInfoModel>();

        public bool CanApprove { get; set; }

        public bool CanGenerate { get; set; }

        public bool CanDelete { get; set; }

        public bool IsGenerated { get; set; }

        public bool IsApproved { get; set; }

        public bool NeedsAttention { get; set; }

        public bool HasErrors { get; set; }

        public bool HasWarnings { get; set; }
    }
}
