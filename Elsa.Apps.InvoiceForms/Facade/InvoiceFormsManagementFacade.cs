using Elsa.Apps.Reporting;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Elsa.Apps.InvoiceForms.Facade
{
    public class InvoiceFormsManagementFacade
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ILog _log;


        public InvoiceFormsManagementFacade(IDatabase database, ISession session, ILog log)
        {
            _database = database;
            _session = session;
            _log = log;
        }

        public List<InvoiceFormCollectionViewModel> GetCollections()
        {
            _session.EnsureUserRight(ReportingUserRights.ManageInvoicingFormPackages);

            var data = _database.Sql().ExecuteWithParams("select Month, Year, PublicUid\r\nfrom FinDataGenerationClosure\r\nwhere ProjectId = {0}\r\norder by CloseDt desc", _session.Project.Id)
                .AutoMap<InvoiceFormCollectionViewModel>();

            var latest = data.FirstOrDefault();

            if (latest != null)
                latest.CanDelete = true;

            return data;
        }

        public void DeletePackage(string publicUid)
        {
            var context = $"ProjectId={_session.Project.Id}, UserId={_session.User.Id}, PublicUid={publicUid}";
            _log.Info($"Mazání balíčku účetních dat zahájeno: {context}, Utc={DateTime.UtcNow:O}");

            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    var collections = GetCollections();

                    var toDel = collections.FirstOrDefault(c => c.PublicUid == publicUid && c.CanDelete);
                    if (toDel == null)
                        throw new ArgumentException("Smazání nebylo možné");

                    _database.Sql().Call("sp_rollbackFinData")
                        .WithParam("@projectId", _session.Project.Id)
                        .WithParam("@year", toDel.Year)
                        .WithParam("@month", toDel.Month)
                        .NonQuery();

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Mazání balíčku účetních dat selhalo: {context}, Utc={DateTime.UtcNow:O}", ex);
                throw;
            }

            _log.Info($"Mazání balíčku účetních dat úspěšně dokončeno: {context}, Utc={DateTime.UtcNow:O}");
        }

        public class InvoiceFormCollectionViewModel
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public string PublicUid { get; set; }

            public string Title => $"{Month.ToString().PadLeft(2, '0')}/{Year}"; 

            public bool CanDelete { get; set; }

            public string DownloadLink => $"/invoiceForms/getpackage?cid={PublicUid}";
        }
    }
}
