using Elsa.App.Crm.DataReporting;
using Elsa.App.Crm.ReportBuilder;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm
{
    [Controller("crmreporting")]
    public class CrmReportingController : ElsaControllerBase
    {
        private readonly DatasetLoader _datasetLoader;

        public CrmReportingController(IWebSession webSession, ILog log, DatasetLoader datasetLoader) : base(webSession, log)
        {
            _datasetLoader = datasetLoader;
        }

        public FileResult GetReport() 
        {
            var ds = _datasetLoader.Execute("CRM_GetDistributorReport", new Dictionary<string, object> { { "@customerId", 27771 } });

            using (var report = new ReportPackage(@"C:\Elsa\ReportTemplates\DistributorsReportTemplate.xlsx"))
            {
                report
                .Insert(0, "A3", ds.Tables[0], headers: false, copyStyle: true)
                .Insert(1, "A4", ds.Tables[1], headers: false, copyStyle: true)
                .Insert(2, "A4", ds.Tables[2], headers: true, copyStyle: true)
                .Insert(3, "A4", ds.Tables[3], headers: true, copyStyle: true);

                return new FileResult($"{Guid.NewGuid()}.xlsx", report.GetBytes());
            }            
        }

        public FileResult GetSalesRepReport() 
        {
            // CRM_GetSalesRepresentativeReport(@salesRepId INT, @startDt DATETIME, @endDt DATETIME)
            var ds = _datasetLoader.Execute("CRM_GetSalesRepresentativeReport", new Dictionary<string, object> {
                { "@salesRepId", 1 },
                {"@startDt", new DateTime(2023, 5, 1) },
                {"@endDt", new DateTime(2023, 7, 31) }});

            using (var report = new ReportPackage(@"C:\Elsa\ReportTemplates\SalesRepresentativeReportTemplate.xlsx"))
            {
                report.
                 Insert(0, "A3", ds.Tables[0], headers: false, copyStyle: false)
                .Insert(0, "A7", ds.Tables[1], headers: false, copyStyle: false)
                .Insert(0, "A13", ds.Tables[2], headers: false, copyStyle: true)

                .Insert(1, "A3", ds.Tables[0], headers: false, copyStyle: false)
                .Insert(1, "A8", ds.Tables[3], headers: true, copyStyle: true);

                return new FileResult($"{Guid.NewGuid()}.xlsx", report.GetBytes());
            }
        }
    }
}
