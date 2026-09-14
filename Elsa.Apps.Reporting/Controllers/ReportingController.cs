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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Reporting.Controllers
{

    [Controller("reporting")]
    public class ReportingController : ElsaControllerBase
    {
        private const string BULLETIN_DIRECTORY = @"C:\Elsa\Bulletin";
        private readonly IDatabase _database;
        private readonly IWebSession _session;
        private readonly Repo.ReportRepository _reportRepository;

        public ReportingController(IWebSession webSession, ILog log, IDatabase database, Repo.ReportRepository reportRepository) : base(webSession, log)
        {
            _database = database;
            _session = webSession;
            _reportRepository = reportRepository;
        }

        public FileResult GetLatestBulletin()
        {
            EnsureUserRight(ReportingUserRights.ViewBulletin);

            var latest = Directory.Exists(BULLETIN_DIRECTORY)
                ? Directory.EnumerateFiles(BULLETIN_DIRECTORY, "bulletin_*.html")
                    .Where(path => DateTime.TryParseExact(
                        Path.GetFileNameWithoutExtension(path).Substring("bulletin_".Length),
                        "yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out _))
                    .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                    .FirstOrDefault()
                : null;

            if (latest == null)
            {
                const string EMPTY_MESSAGE = "<!DOCTYPE html><html lang=\"cs\"><head><meta charset=\"utf-8\">"
                    + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
                    + "<title>Bulletin</title></head><body><h1>Bulletin</h1>"
                    + "<p>Zatím nebyl vygenerován žádný Bulletin. Zkuste to prosím později.</p></body></html>";
                return new FileResult("bulletin.html", Encoding.UTF8.GetBytes(EMPTY_MESSAGE),
                    "text/html; charset=utf-8", "inline") { DisableBrowserCache = true };
            }

            return new FileResult(Path.GetFileName(latest), File.ReadAllBytes(latest),
                "text/html; charset=utf-8", "inline") { DisableBrowserCache = true };
        }

        public List<ReportTypeModel> GetReportTypes()
        {
            EnsureUserRight(ReportingUserRights.ReportingApp);

            return _reportRepository.GetReportTypes();
        }

        public FileResult GetReport(string code)
        {
            EnsureUserRight(ReportingUserRights.ReportingApp);

            var rt = GetReportTypes().FirstOrDefault(r => r.Code == code);
            if (rt == null)
                throw new ArgumentException("Neznámý kód reportu");

            byte[] bytes;
            using (var report = _database.Sql().Call(rt.Code).WithParam("@projectId", _session.Project.Id)
                .Table())
            {
                DynamicColumnNamesProcessor.SetDynamicColumnNames(report);

                bytes = GetXlsData(rt, report);
            }

            return new FileResult(StringUtil.SanitizeFileName($"{rt.Title}.xlsx"), bytes);
        }

        private byte[] GetXlsData(ReportTypeModel type, DataTable table)
        {
            using (var package = new ExcelPackage())
            {
                var reportSheet = package.Workbook.Worksheets.Add(type.Title.Limit(100));
                reportSheet.Cells["A1"].LoadFromDataTable(table, true);

                var metadataSheet = package.Workbook.Worksheets.Add("Metadata");
                metadataSheet.Cells["A1"].Value = "Vygenerováno";
                metadataSheet.Cells["B1"].Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                metadataSheet.Cells["A2"].Value = "Popis";
                metadataSheet.Cells["B2"].Value = type.Note;

                return package.GetAsByteArray();         
            }
        }
    }
}
