using Elsa.Apps.Reporting.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using OfficeOpenXml;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Reporting.Controllers
{

    [Controller("reporting")]
    public class ReportingController : ElsaControllerBase
    {
        private readonly IDatabase m_database;
        private readonly IWebSession m_session;
        private readonly Repo.ReportRepository m_reportRepository;

        public ReportingController(IWebSession webSession, ILog log, IDatabase database, Repo.ReportRepository reportRepository) : base(webSession, log)
        {
            m_database = database;
            m_session = webSession;
            m_reportRepository = reportRepository;
        }

        public List<ReportTypeModel> GetReportTypes()
        {
            EnsureUserRight(ReportingUserRights.ReportingApp);

            return m_reportRepository.GetReportTypes();                
        }

        public FileResult GetReport(string code)
        {
            EnsureUserRight(ReportingUserRights.ReportingApp);

            var rt = GetReportTypes().FirstOrDefault(r => r.Code == code);
            if (rt == null)
                throw new ArgumentException("Neznámý kód reportu");

            byte[] bytes;
            using (var report = m_database.Sql().Call(rt.Code).WithParam("@projectId", m_session.Project.Id)
                .Table())
            {
                DynamicColumnNamesProcessor.SetDynamicColumnNames(report);

                bytes = GetXlsData(rt.Code, report);
            }

            return new FileResult(StringUtil.SanitizeFileName($"{rt.Title}.xlsx"), bytes);
        }

        private byte[] GetXlsData(string title, DataTable table)
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add(title);
                sheet.Cells["A1"].LoadFromDataTable(table, true);

                return package.GetAsByteArray();         
            }
        }
    }
}
