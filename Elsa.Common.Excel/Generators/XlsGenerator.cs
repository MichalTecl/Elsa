using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.XTable.Model;

using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;

namespace Elsa.Common.XTable.Generators
{
    public static class XlsGenerator
    {
        public static byte[] CreateExcel(XWorkbook data)
        {
            using (var p = new ExcelPackage())
            {
                foreach (var srcSheet in data.Sheets)
                {
                    var excelSheet = p.Workbook.Worksheets.Add(string.IsNullOrWhiteSpace(srcSheet.Data)
                        ? $"Worksheet{p.Workbook.Worksheets.Count + 1}"
                        : srcSheet.Data);
                    CreateData(srcSheet, excelSheet);
                }

                return p.GetAsByteArray();
            }
        }

        private static void CreateData(XSheet srcSheet, ExcelWorksheet excelSheet)
        {
            for (var rowIndex = 0; rowIndex < srcSheet.Rows.Count; rowIndex++)
            {
                var row = srcSheet.Rows[rowIndex];

                for (var colIndex = 0; colIndex < row.Cells.Count; colIndex++)
                {
                    excelSheet.Cells[rowIndex + 1, colIndex + 1].Value = row.Cells[colIndex].Value;
                }
            }
        }
    }
}
