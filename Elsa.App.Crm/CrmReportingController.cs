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

            using (var report = new ReportPackage(@"C:\Elsa\ReportTemplates\CrmReport1Template.xlsx"))
            {
                report
                .Insert("Informace o VO partnerovi", "A3", ds.Tables[0], headers: false, copyStyle: true)
                .Insert("Všechny objednávky VO", "A4", ds.Tables[1], headers: false, copyStyle: true)
                .Insert("Odběr - produkty", "A4", ds.Tables[2], headers: true, copyStyle: true)
                .Insert("Odběr - skupiny", "A4", ds.Tables[3], headers: true, copyStyle: true);

                return new FileResult($"{Guid.NewGuid()}.xlsx", report.GetBytes());
            }            
        }
    }
}
