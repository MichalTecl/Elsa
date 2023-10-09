using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;
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
        private readonly ICustomerRepository _customerRepository;
        private readonly ILog _log;
        private readonly ICache _cache;

        public RayNetDistributorsPush(IRaynetClient raynet, IDatabase db, ISession session, ICustomerRepository customerRepository, ILog log, ICache cache)
        {
            _raynet = raynet;
            _db = db;
            this._session = session;
            _customerRepository = customerRepository;
            _log = log;
            _cache = cache;
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

            var rnCats = GetRnCompanyCategories();

            foreach (var c in GetCustomerGroups(e.Id).Where(cg => rnCats.Any(rnCat => cg.Equals(rnCat.Code01, StringComparison.InvariantCultureIgnoreCase))))
                yield return c;

            var srepIndex = _customerRepository.GetCustomerSalesRepresentativeEmailIndex();
            if (srepIndex.TryGetValue(e.Id, out var srepEmail))
                yield return srepEmail;

            var dadr = GetDeliveryAddress(e.Id);
            if (dadr != null) 
            {
                yield return dadr.Street;
                yield return dadr.DescriptiveNumber;
                yield return dadr.OrientationNumber;
                yield return dadr.City;
                yield return dadr.Zip;
                yield return dadr.Phone;
            }            
        }

        public long GetEntityId(ICustomer ett)
        {
            return ett.Id;
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

        private Dictionary<int, IAddress> _deliveryAddresses = null;
        private IAddress GetDeliveryAddress(int customerId) 
        {
            _deliveryAddresses = _deliveryAddresses ?? _customerRepository.GetDistributorDeliveryAddressesIndex();

            if (_deliveryAddresses.TryGetValue(customerId, out var address))
                return address;

            return null;
        }

        public EntityChunk<ICustomer> LoadChunkToCompare(IDatabase db, int projectId, EntityChunk<ICustomer> previousChunk, int alreadyProcessedRowsCount)
        {
            var data = db.SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == projectId)
                .Where(c => c.IsDistributor)
                //.Where(c => c.DisabledDt == null)
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

                string extId = null;

                try 
                {
                    var existing = TryLoadExistingCustomer(e);

                    if (existing == null)
                    {
                        extId = InsertRnContact(e, log, callback);
                        log.Info($"{e.Entity.Name} inserted to RayNet");
                    }
                    else
                    {
                        var updated = CustomerMapper.ToRaynetContact(e.Entity, GetCustomerGroups(e.Entity.Id), GetRnCompanyCategories(), GetDeliveryAddress(e.Entity.Id), GetContactSourceId(e.Entity.Id), existing);
                        _raynet.UpdateContact(existing.Id.Value, updated);

                        UpdateClientAddresses(updated, log);

                        log.Info($"{e.Entity.Name} updated in RayNet");
                        extId = existing.Id.ToString();
                    }

                    callback.OnProcessed(e.Entity, extId, null);                                        
                }
                catch(Exception ex) 
                {
                    log.Error($"Attempt to push a client to RayNet failed: {ex.Message}", ex);                        
                }                   
            }            
        }

        private ContactDetail TryLoadExistingCustomer(EntityChangeEvent<ICustomer> e)
        {
            if (!e.IsNew)
            {
                if (!long.TryParse(e.ExternalId, out var srcId))
                {
                    _log.Error($"Cannot parse ExternalId=\"{e.ExternalId}\" as long");
                }
                else
                {
                    try
                    {
                        var byId = _raynet.GetContactDetail(srcId);
                        if (byId.Data != null)
                            return byId.Data;
                    }
                    catch (RaynetException ex)
                    {
                        _log.Error($"Failed to get Raynet contact by Id={srcId}", ex);
                    }
                }          
            }

            var ids = new List<long>();

            ids.AddRange(_raynet.GetContacts(regNumber: e.Entity.CompanyRegistrationId).Data.Select(d => d.Id.Value));
            
            if (!ids.Any())
                ids.AddRange(_raynet.GetContacts(name: e.Entity.Name).Data.Select(d => d.Id.Value));

            if(!ids.Any())
                ids.AddRange(_raynet.GetContacts(fulltext: e.Entity.Name).Data.Select(d => d.Id.Value));

            if (ids.Any())
            {
                _log.Error($"Unexpectedly found client in Raynet which was assumed to be new ({e.Entity.Name ?? e.Entity.Email})");

                var loadedDetails = new List<ContactDetail>();

                foreach (var id in ids.Distinct())
                {
                    var detail = _raynet.GetContactDetail(id);
                    if (detail.Data.Addresses.Any(a => a.ContactInfo?.Email?.Equals(e.Entity.Email, StringComparison.InvariantCultureIgnoreCase) == true))
                        return detail.Data;

                    loadedDetails.Add(detail.Data);
                }

                var byName = loadedDetails.FirstOrDefault(d => d.Name.Equals(e.Entity.Name, StringComparison.InvariantCultureIgnoreCase));
                if (byName != null)
                    return byName;

                var byReg = loadedDetails.FirstOrDefault(d => d.RegNumber == e.Entity.CompanyRegistrationId);
                if (byReg != null)
                    return byReg;

                _log.Error("No RN record matched detailed check - check weird case!");
            }

            return null;
        }

        private void UpdateClientAddresses(ContactDetail updated, ILog log)
        {
            var current = _raynet.GetContactDetail(updated.Id.Value);

            foreach(var addressToSave in updated.Addresses.Where(a => a.Address.Name == CustomerMapper.DELIVERY_ADDRESS_NAME || a.Address.Name == CustomerMapper.PRIMARY_ADDRESS_NAME)) 
            {
                var existingAddress = current.Data.Addresses.FirstOrDefault(a => a.Address.Name == addressToSave.Address.Name);
                if (existingAddress == null) 
                {
                    log.Info($"Adding address {addressToSave.Address.Name} to customer {updated.Id} {updated.Name}");
                    _raynet.AddContactAddress(updated.Id.Value, addressToSave);
                    log.Info($"Added address {addressToSave.Address.Name} to customer {updated.Id} {updated.Name}");
                }
                else if(!existingAddress.IsSameAs(addressToSave))
                {
                    log.Info($"Updating address {addressToSave.Address.Name} for customer {updated.Id} {updated.Name}");
                    _raynet.UpdateContactAddress(updated.Id.Value, existingAddress.Id.Value, existingAddress);
                    log.Info($"Updated address {addressToSave.Address.Name} for customer {updated.Id} {updated.Name}");
                }
            }
        }

        private string InsertRnContact(EntityChangeEvent<ICustomer> e,  ILog log, IEntityProcessCallback<ICustomer> callback)
        {
            var resp = _raynet.InsertContact(CustomerMapper.ToRaynetContact(e.Entity, GetCustomerGroups(e.Entity.Id), GetRnCompanyCategories(), GetDeliveryAddress(e.Entity.Id), GetContactSourceId(e.Entity.Id)));
            log.Info($"{e.Entity.Name} inserted to RayNet");
            return resp.Data.Id.ToString();
        }

        private long? GetContactSourceId(int elsaCustomerId) 
        {
            var index = _customerRepository.GetCustomerSalesRepresentativeEmailIndex();

            if(!index.TryGetValue(elsaCustomerId, out var srepEmail) || string.IsNullOrWhiteSpace(srepEmail))
            {
                _log.Info($"No SalesRepresentative found for userId={elsaCustomerId}");
                return null;
            }

            return _cache.ReadThrough<long>($"raynet_csrc{_session.Project.Id}{srepEmail}", TimeSpan.FromMinutes(1), () => 
            {
                var sources = _raynet.GetContactSources().Data;

                var csrc = sources.FirstOrDefault(s => s.Code01.Equals(srepEmail, StringComparison.InvariantCultureIgnoreCase));
                if (csrc != null)
                    return csrc.Id;

                _log.Info($"Contact source \"{srepEmail}\" does not exist in RayNet - creating");
                return _raynet.CreateContactSource(srepEmail).Data;
            });
        }
    }
}
