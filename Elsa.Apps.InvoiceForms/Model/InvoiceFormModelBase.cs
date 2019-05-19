using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class InvoiceFormModelBase
    {
        public int InvoiceFormId { get; set; }

        public string IssueDate { get; set; }

        public string InvoiceFormNumber { get; set; }

        public decimal PrimaryCurrencyPriceWithoutVat { get; set; }

        public string FormattedPrimaryCurrencyPriceWithoutVat { get; set; }

        public string OriginalCurrencyPrice { get; set; }

        public string OriginalCurrencySymbol { get; set; }

        public string ConversionRateLink { get; set; }

        public string ConversionRate { get; set; }

        public bool IsCanceled { get; set; }
    }
}
