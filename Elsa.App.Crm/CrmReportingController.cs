using Elsa.App.Crm.DataReporting;
using Elsa.App.Crm.Model;
using Elsa.App.Crm.ReportBuilder;
using Elsa.App.Crm.Repositories;
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
        private readonly SalesRepRepository _salesReps;

        public CrmReportingController(IWebSession webSession, ILog log, DatasetLoader datasetLoader, SalesRepRepository salesReps) : base(webSession, log)
        {
            _datasetLoader = datasetLoader;
            _salesReps = salesReps;
        }

        public FileResult getDistributorReport(int distributorId) 
        {
            var ds = _datasetLoader.Execute("CRM_GetDistributorReport", new Dictionary<string, object> { { "@customerId", distributorId} });

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

        public FileResult GetSalesRepReport(int? salesRepId, string dtFrom, string dtTo) 
        {
            var from = DateTime.Parse(dtFrom);
            var to = DateTime.Parse(dtTo);

            // CRM_GetSalesRepresentativeReport(@salesRepId INT, @startDt DATETIME, @endDt DATETIME)
            var ds = _datasetLoader.Execute("CRM_GetSalesRepresentativeReport", new Dictionary<string, object> {
                { "@salesRepId", salesRepId },
                {"@startDt", from },
                {"@endDt", to }});

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

        public IEnumerable<SalesRepresentativeModel> GetSalesReps() 
        {
            return _salesReps.GetSalesRepresentatives(null).Select(i => new SalesRepresentativeModel
            {
                Id = i.Id,
                Name = i.PublicName
            });
        }

        public IEnumerable<DistributorViewModel> GetDistributors() 
        {
            var reps = _salesReps.GetSrCustomers();

            foreach(var d in _salesReps.GetDistributors(null).ToList()) 
            {
                var sr = reps.FirstOrDefault(r => r.CustomerId == d.Id);
                if (sr == null)
                    continue;

                yield return new DistributorViewModel
                {
                    Id = d.Id,
                    Name = d.Name ?? d.Email,
                    SalesRepId = sr.SalesRepId
                };
            }
        }
    }
}
