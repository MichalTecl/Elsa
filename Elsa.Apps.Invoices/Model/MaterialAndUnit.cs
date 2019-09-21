using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Invoices.Model
{
    public class MaterialAndUnit
    {
        [XlsColumn(0)]
        public string MaterialName { get; set; }

        [XlsColumn(1)]
        public string Unit { get; set; }
    }
}
