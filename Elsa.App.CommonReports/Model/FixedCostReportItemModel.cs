using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    public class FixedCostReportItemModel
    {
        [XlsColumn("A", "Měsíc", "@")]
        public string Month { get; set; }

        [XlsColumn("B", "Popis", "@")]
        public string TypeName { get; set; }

        [XlsColumn("C", "Celkem CZK", "0.00")]
        public decimal Value { get; set; }

        [XlsColumn("D", "Podíl k rozpočtení", "0.00%")]
        public decimal Percent { get; set; }

        [XlsColumn("E", "Rozpočteno", "0.00")]
        [R1C1Formula("R[0]C[-1] * R[0]C[-2]")]
        public decimal Distributed { get; set; }
    }
}
