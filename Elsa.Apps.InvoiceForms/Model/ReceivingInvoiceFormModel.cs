using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class ReceivingInvoiceFormModel : InvoiceFormModelBase
    {
        public string Supplier { get; set; }
        
        public string InvoiceVarSymbol { get; set; }
    }
}
