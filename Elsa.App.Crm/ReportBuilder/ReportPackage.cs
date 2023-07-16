using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace Elsa.App.Crm.ReportBuilder
{
    public class ReportPackage : IDisposable
    {
        private readonly string _templatePath;
        private readonly ExcelPackage _excelPackage;

        public ReportPackage(string templatePath)
        {
            _templatePath = templatePath;
            _excelPackage = new ExcelPackage(new System.IO.FileInfo(templatePath), true);
        }

        public void Dispose()
        {
            _excelPackage?.Dispose();
        }

        public ReportPackage Insert(int sheetIndex, string startCell, DataTable table, bool headers = true, bool copyStyle = false) 
        {            
            var sheet = _excelPackage.Workbook.Worksheets[sheetIndex + 1];

            var dataRowOffset = headers ? 1 : 0;

            var homeCell = sheet.Cells[startCell];
            var firstDataRowAddress = ExcelRange.GetAddress(homeCell.Start.Row + dataRowOffset, homeCell.Start.Column, homeCell.Start.Row + dataRowOffset, homeCell.Start.Column + table.Columns.Count - 1);

            if (copyStyle) 
            {              

                var firstDataRow = sheet.Cells[firstDataRowAddress];

                for(var i = 1; i < table.Rows.Count; i++) 
                {
                    var copyTargetAddress = ExcelRange.GetAddress(firstDataRow.Start.Row + i, firstDataRow.Start.Column, firstDataRow.End.Row + i, firstDataRow.End.Column);
                    firstDataRow.Copy(sheet.Cells[copyTargetAddress]);
                }
            }

            if (table.Rows.Count == 0) 
            {
                sheet.Cells[firstDataRowAddress].Clear();
            }

            sheet.Cells[startCell].LoadFromDataTable(table, headers);

            return this;
        }

        public byte[] GetBytes() 
        {
            return _excelPackage.GetAsByteArray();
        }



    }
}
