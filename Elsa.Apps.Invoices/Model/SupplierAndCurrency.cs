using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Invoices.Model
{
    public class SupplierAndCurrency
    {
        [XlsColumn(0)]
        public string SupplierName { get; set; }

        [XlsColumn(1)]
        public string Currency { get; set; }
    }
}
