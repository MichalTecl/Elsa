using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.CommonData.ExcelInterop;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.SaleEvents.Model.Xls
{
    [HeaderStyle(BackgroundColor = "Blue", Color="White", FontStyle = FontStyle.Bold, Locked = true)]
    public class SaleEventAllocationModel
    {
        [XlsColumn("A", "Materiál")]
        [ValidateMaterial]
        public string MaterialName { get; set; }

        [XlsColumn("B", "Šarže")]
        public string BatchNumber { get; set; }

        [XlsColumn("C", "Odebráno")]
        public decimal AllocatedQuantity { get; set; }

        [XlsColumn("D", "Jednotka")]
        [SetUnitByMaterial("R[0]C[-3]")]
        public string AllocationUnitSymbol { get; set; }

        [XlsColumn("E", "Vráceno")]
        public decimal? ReturnQuantity { get; set; }
    }
}
