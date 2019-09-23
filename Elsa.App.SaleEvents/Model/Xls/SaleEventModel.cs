using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.CommonData.ExcelInterop;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.SaleEvents.Model.Xls
{
    [XlsSheet(0, "Prodej")]
    [LabelStyle(FontStyle = FontStyle.Bold, Locked = true)]
    public class SaleEventModel : ElsaExcelModelBase
    {
        [XlsCell("B1")]
        [Label("Id")]
        [CellStyle(Locked = true)]
        public int Id { get; set; }

        [XlsCell("B2")]
        [Label("Název")]
        public string Name { get; set; }

        [XlsCell("B3", ExcelDateFormat)]
        [Label("Datum blokace")]
        public string AllocDate { get; set; }

        [XlsCell("B4", ExcelDateFormat)]
        [Label("Datum vrácení")]
        public string ReturnDate { get; set; }

        [XlsCell("A6")]
        public List<SaleEventAllocationModel> Items { get; } = new List<SaleEventAllocationModel>();
    }
}
