using Elsa.App.Crm;
using Elsa.App.PublicFiles;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Jobs.BuildStoresMap.Config;
using Elsa.Jobs.Common;
using Robowire.RobOrm.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Elsa.Jobs.BuildStoresMap
{
    public class BuildMapJob : IExecutableJob
    {
        private readonly IDatabase _db;
        private readonly ISession _session;
        private readonly ILog _log;
        private readonly IPublicFilesHelper _publicFiles;
        private readonly StoreMapConfig _config;

        public BuildMapJob(IDatabase db, ISession session, ILog log, IPublicFilesHelper publicFiles, StoreMapConfig config)
        {
            _db = db;
            _session = session;
            _log = log;
            _publicFiles = publicFiles;
            _config = config;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Running procedure MarkValuableDistributors");
            var res = _db.Sql().Call("MarkValuableDistributors")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@minOrdersCount", _config.MinOrdersCount)
                .WithParam("@maxMonthsFromLastOrder", _config.MaxMonthsFromLastOrder)
                .NonQuery();
            _log.Info($"MarkValuableDistributors returned {res}");

            _log.Info("Loading Elsa Db contacts (Valuable Distributors)");
            var dbContacts = _db.SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == _session.Project.Id)
                .Where(c => c.IsValuableDistributor == true)
                .Execute()
                .ToList();

            _log.Info($"Loaded {dbContacts.Count} contacts");
                                                
            foreach(var dbContact in dbContacts)
            {
                if (string.IsNullOrWhiteSpace(dbContact.CompanyRegistrationId))
                {
                    _log.Error($"Valuable distributor {dbContact.Name} has no company registration id");
                    continue;
                }                
            }

            GenerateCsv();
        }
               
        private void GenerateCsv()
        {
            var customerName = _session.Project.Name;

            _publicFiles.Write(customerName, "StoreMap", "storemap.csv", writer =>
            {
                var csv = new CsvGenerator(writer, new[] { "title", "address", "city", "url", "preview", "lat", "lng" });

                var cGroupTypes = _db.SelectFrom<ICustomerGroupType>().Execute().ToList();

                var stores = _db.SelectFrom<ICustomerStore>()
                    .Join(s => s.Customer)
                    .Join(s => s.Customer.CustomerGroups)
                    .Where(s => s.Customer.ProjectId == _session.Project.Id)
                    .Where(s => s.Customer.IsValuableDistributor == true)
                    .Where(s => s.Customer.HasStore == true)
                    .Execute()
                    .ToList();

                foreach (var store in stores)
                {
                    bool skip = false;

                    if (store.Customer.CustomerGroups != null)
                    {
                        foreach(var cGroup in store.Customer.CustomerGroups)
                        {
                            var gType = cGroupTypes.FirstOrDefault(gt => gt.ErpGroupName.Equals(cGroup.ErpGroupName, StringComparison.InvariantCultureIgnoreCase));
                            if (gType?.IsDisabled == true)
                            {
                                skip = true;
                                break;
                            }
                        }
                    }

                    if (skip)
                        continue;

                    try
                    {
                        csv.CellMan(store.Name)
                            .CellMan(store.Address)
                            .CellMan(store.City)
                            .CellMan(store.Www)
                            .CellMan(store.PreviewName)
                            .CellMan(store.Lat)
                            .CellMan(store.Lon);

                        csv.CommitRow();
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Error writing store {store.Id} to csv: {ex.Message}");
                        csv.RollbackRow();
                    }
                }
            });           
        }
    }
}
