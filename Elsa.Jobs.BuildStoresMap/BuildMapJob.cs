using Elsa.App.PublicFiles;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Crm.Raynet.Model;
using Elsa.Jobs.BuildStoresMap.Config;
using Elsa.Jobs.BuildStoresMap.Entities;
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
        private const string StorePrefix = "Prodejna";

        private readonly IRaynetClient _raynet;
        private readonly IDatabase _db;
        private readonly ISession _session;
        private readonly ILog _log;
        private readonly IPublicFilesHelper _publicFiles;
        private readonly StoreMapConfig _config;

        public BuildMapJob(IRaynetClient raynet, IDatabase db, ISession session, ILog log, IPublicFilesHelper publicFiles, StoreMapConfig config)
        {
            _raynet = raynet;
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

            _log.Info("Loading Elsa Db stores");
            var dbStores = _db.SelectFrom<ICustomerStore>()
                .Join(s => s.Customer)
                .Where(s => s.Customer.ProjectId == _session.Project.Id)
                .Execute()
                .ToList();
            _log.Info($"Loaded {dbStores.Count} stores");
                        
            foreach(var dbContact in dbContacts)
            {
                if (string.IsNullOrWhiteSpace(dbContact.CompanyRegistrationId))
                {
                    _log.Error($"Valuable distributor {dbContact.Name} has no company registration id");
                    continue;
                }

                var raynetContacts = _raynet.GetContacts(regNumber: dbContact.CompanyRegistrationId);
                if (raynetContacts.Data.Count != 1)
                {
                    _log.Error($"Valuable distributor {dbContact.Id} has {raynetContacts.Data.Count} contacts in Raynet");
                    continue;
                }

                var detail = _raynet.GetContactDetail(raynetContacts.Data[0].Id.Value);
                if (detail == null)
                {
                    _log.Error($"Valuable distributor {dbContact.Id} has no detail in Raynet");
                    continue;
                }

                var eshop = _raynet.ReadCustomField<bool?>(detail.Data, "E-shop") == true;
                var store = _raynet.ReadCustomField<bool?>(detail.Data, "Kamenná prodejna") == true;

                var changed = false;
                if (dbContact.HasEshop != eshop)
                {
                    dbContact.HasEshop = eshop;
                    changed = true;
                }

                if (dbContact.HasStore != store)
                {
                    dbContact.HasStore = store;
                    changed = true;
                }

                if (changed)
                {
                    _db.Save(dbContact);
                    _log.Info($"Valuable distributor {dbContact.Name} updated: HasEshop={dbContact.HasEshop}, HasStore={dbContact.HasStore}");
                }   

                var savedCustomerStores = dbStores.Where(s => s.CustomerId == dbContact.Id).ToList();
                var raynetStores = detail.Data.Addresses.Where(a => a.Address.Name.StartsWith(StorePrefix)).ToList();
                                
                foreach(var savedStore in savedCustomerStores)
                {
                    var rnStore = raynetStores.FirstOrDefault(rs => rs.Address.Name == savedStore.SystemRecordName);
                    if (rnStore == null)
                    {
                        _db.Delete(savedStore);
                        _log.Info($"Store {savedStore.SystemRecordName} - {savedStore.Name} deleted for customer {detail.Data.Name}");

                        continue;
                    }

                    if (MapStore(detail.Data, savedStore))
                    {
                        _db.Save(savedStore);
                        _log.Info($"Store {savedStore.SystemRecordName} - {savedStore.Name} updated for customer {detail.Data.Name}");
                    }                    
                }

                foreach(var rnStore in raynetStores)
                {
                    if (savedCustomerStores.Any(s => s.SystemRecordName == rnStore.Address.Name))
                    {
                        continue;
                    }

                    var newStore = _db.New<ICustomerStore>();
                    newStore.CustomerId = dbContact.Id;
                    newStore.SystemRecordName = rnStore.Address.Name;
                    MapStore(detail.Data, newStore);
                    
                    _db.Save(newStore);
                    _log.Info($"Store {newStore.SystemRecordName} - {newStore.Name} added for customer {detail.Data.Name}");                    
                }                
            }

            GenerateCsv();
        }

        private bool MapStore(ContactDetail contact, ICustomerStore target)
        {
            var sourceAddress = contact.Addresses.FirstOrDefault(a => a.Address.Name == target.SystemRecordName) 
                ?? throw new Exception("No source address");

            bool set(string source, Func<ICustomerStore, string> getter, Action<ICustomerStore, string> setter)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    return false;
                }

                if (source != getter(target))
                {
                    setter(target, source);
                    return true;
                }

                return false;
            }

            var name = sourceAddress.Address.Name.Substring(StorePrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = contact.Name;

            return set(name, s => s.Name, (s,v) => s.Name = v)
                | set(sourceAddress.Address.Street, s => s.Address, (s,v) => s.Address = v)
                | set(sourceAddress.Address.City, s => s.City, (s,v) => s.City = v)
                | set(sourceAddress.ContactInfo.Www ?? contact.Addresses.Select(a => a.ContactInfo?.Www).FirstOrDefault(w => !string.IsNullOrWhiteSpace(w)), s => s.Www, (s,v) => s.Www = v)
                | set(name, s => s.PreviewName, (s, v) => s.PreviewName = v)
                | set(sourceAddress.Address.Lat.ToString(), s => s.Lat, (s,v) => s.Lat =v)
                | set(sourceAddress.Address.Lng.ToString(), s => s.Lon, (s, v) => s.Lon = v);               
        }

        private void GenerateCsv()
        {
            var customerName = _session.Project.Name;

            _publicFiles.Write(customerName, "StoreMap", "storemap.csv", writer =>
            {
                var csv = new CsvGenerator(writer, new[] { "title", "address", "city", "url", "preview", "lat", "lng" });

                var stores = _db.SelectFrom<ICustomerStore>()
                    .Join(s => s.Customer)
                    .Where(s => s.Customer.ProjectId == _session.Project.Id)
                    .Where(s => s.Customer.IsValuableDistributor == true)
                    .Where(s => s.Customer.HasStore == true)
                    .Execute()
                    .ToList();

                foreach (var store in stores)
                {
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
