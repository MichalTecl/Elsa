using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Crm.Raynet.Model;
using Elsa.Jobs.Common.EntityChangeProcessing;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Elsa.Jobs.ExternalSystemsDataPush.Mappers;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ExternalSystemsDataPush.ChangeProcessors
{
    public class RayNetDistributorsPush : IEntityChangeProcessor<ICustomer>
    {
        private readonly IRaynetClient _raynet;
        private readonly IDatabase _db;
        private readonly ISession _session;

        public RayNetDistributorsPush(IRaynetClient raynet, IDatabase db, ISession session)
        {
            _raynet = raynet;
            _db = db;
            this._session = session;
        }

        public string ProcessorUniqueName { get; } = "Raynet_Customers_Push";

        public IEnumerable<object> GetComparedValues(ICustomer e)
        {
            yield return e.Email;
            yield return e.Phone;
            yield return e.Name;
            yield return e.NewsletterSubscriber;
            yield return e.IsDistributor;
            yield return e.VatId;
            yield return e.CompanyName;
            yield return e.Street;
            yield return e.DescriptiveNumber;
            yield return e.OrientationNumber;
            yield return e.City;
            yield return e.Zip;
            yield return e.Country;
            yield return e.GetFormattedStreetAndHouseNr();

            foreach (var c in GetCustomerGroups(e.Id))
                yield return c;            
        }

        public long GetEntityId(ICustomer ett)
        {
            return ett.Id;
        }

        private List<Contact> _rncontacts = null;
        private List<Contact> GetRnContacts()
        {
            if (_rncontacts == null) 
            {
                var lst = new List<Contact>();
                while (true)
                {
                    var resp = _raynet.GetContacts(lst.Count, 100);
                    lst.AddRange(resp.Data);

                    if (resp.Data.Count < 100)
                        break;
                }

                _rncontacts = lst;
            }

            return _rncontacts;
        }


        private List<CompanyCategory> _rnCategories = null;
        private List<CompanyCategory> GetRnCompanyCategories() 
        {
            if (_rnCategories == null)
            {
                _rnCategories = _raynet.GetCompanyCategories().Data;
            }

            return _rnCategories;
        }

        private Dictionary<int, HashSet<string>> _customerGroups = null;
        private HashSet<string> GetCustomerGroups(int customerId) 
        {
            if (_customerGroups == null) 
            {
                var grps = _db.SelectFrom<ICustomerGroup>()
                    .Join(cg => cg.Customer)
                    .Where(cg => cg.Customer.ProjectId == _session.Project.Id)
                    .Execute();

                _customerGroups = new Dictionary<int, HashSet<string>>();

                foreach(var record in grps) 
                {
                    if (!_customerGroups.TryGetValue(record.CustomerId, out var groupNames)) 
                    {
                        groupNames = new HashSet<string>();
                        _customerGroups.Add(record.CustomerId, groupNames);
                    }

                    groupNames.Add(record.ErpGroupName);
                }
            }

            if(!_customerGroups.TryGetValue(customerId, out var found)) 
            {
                found = new HashSet<string>();
            }

            return found;
        }

        public EntityChunk<ICustomer> LoadChunkToCompare(IDatabase db, int projectId, EntityChunk<ICustomer> previousChunk, int alreadyProcessedRowsCount)
        {
            var data = db.SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == projectId)
                .Where(c => c.IsDistributor)
                .OrderBy(c => c.Id)
                .Skip(alreadyProcessedRowsCount)
                .Take(100)
                .Execute()                
                .ToList();

            return new EntityChunk<ICustomer>(data.Where(c => c.ErpUid?.StartsWith("C") == true).ToList(), data.Count < 100);
        }

        public void Process(IEnumerable<EntityChangeEvent<ICustomer>> changedEntities, IEntityProcessCallback<ICustomer> callback, ILog log)
        {
            foreach (var e in changedEntities) 
            {
                log.Info($"Processing changed Customer Id={e.Entity.Id}, Name={e.Entity.Name ?? e.Entity.Email}");

                try 
                {
                    if (e.IsNew) 
                    {
                        InsertRnContact(e, log, callback);
                    }
                    else 
                    {
                        if(!long.TryParse(e.ExternalId, out var srcId))
                        {
                            log.Error($"Cannot parse ExternalId=\"{e.ExternalId}\" as long - considering inserting the contact instead of UPDATE");
                            InsertRnContact(e, log, callback);
                            continue;
                        }

                        var source = GetRnContacts().FirstOrDefault(c => c.Id == srcId);
                        if (source == null) 
                        {
                            _rncontacts = null;
                            source = GetRnContacts().FirstOrDefault(c => c.Id == srcId);

                            if (source == null) 
                            {
                                log.Error($"Cannot find source Contact by RayNet_ID={srcId} - considering inserting the contact instead of UPDATE");
                                InsertRnContact(e, log, callback);
                                continue;
                            }                            
                        }

                        var updated = CustomerMapper.ToRaynetContact(e.Entity, GetCustomerGroups(e.Entity.Id), GetRnCompanyCategories(), source);
                        _raynet.UpdateContact(srcId, updated);
                        log.Info($"{e.Entity.Name} updated in RayNet");
                        callback.OnProcessed(e.Entity, srcId.ToString(), null);
                    }                    
                }
                catch(Exception ex) 
                {
                    log.Error($"Attempt to push a client to RayNet failed: {ex.Message}", ex);                        
                }                   
            }            
        }

        private void InsertRnContact(EntityChangeEvent<ICustomer> e,  ILog log, IEntityProcessCallback<ICustomer> callback)
        {
            var resp = _raynet.InsertContact(CustomerMapper.ToRaynetContact(e.Entity, GetCustomerGroups(e.Entity.Id), GetRnCompanyCategories()));
            log.Info($"{e.Entity.Name} inserted to RayNet");
            callback.OnProcessed(e.Entity, resp.Data.Id.ToString(), null);
        }
    }
}
