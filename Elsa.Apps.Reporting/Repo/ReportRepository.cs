using Elsa.Apps.Reporting.Model;
using Elsa.Common.DbUtils;
using Elsa.Common.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Apps.Reporting.Repo
{
    public class ReportRepository
    {
        private readonly IProcedureLister _spLister;

        public ReportRepository(IProcedureLister spLister)
        {
            _spLister = spLister;
        }

        public List<ReportTypeModel> GetReportTypes()
        {
            return _spLister.ListProcedures("xrep_%").Select(CreateType).ToList();
        }


        private static ReportTypeModel CreateType(ProcedureInfo sp)
        {            
            return new ReportTypeModel
            {
                Code = sp.ProcedureName,
                Title = sp.Tags.GetOrDefault("Title", sp.ProcedureName),
                Note = sp.Tags.GetOrDefault("Note")
            };
        }
    }
}
