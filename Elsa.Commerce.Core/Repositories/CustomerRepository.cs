using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Crm.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entitites.Crm;
using Microsoft.SqlServer.Server;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ILog _log;
        private readonly ICache _cache;

        public CustomerRepository(IDatabase database, ISession session, ILog log, ICache cache)
        {
            _database = database;
            _session = session;
            _log = log;
            _cache = cache;
        }

        public void SyncCustomers(IEnumerable<IErpCustomerModel> source)
        {
            var changeLogGroupingTag = $"Import_{Guid.NewGuid()}";

            var allDbCustomers =
                _database.SelectFrom<ICustomer>().Where(c => c.ProjectId == _session.Project.Id).Execute().OrderByDescending(i => i.Id).ToList();

            foreach (var src in source)
            {
                var trg =
                    allDbCustomers.FirstOrDefault(dbc => dbc.ErpUid == src.ErpCustomerId) ??
                    allDbCustomers.FirstOrDefault(s => s.Email.Equals(src.Email, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(s.ErpUid))
                    ?? _database.New<ICustomer>();

                SyncCustomer(src, trg, changeLogGroupingTag);
            }

            try
            {
                _log.Info("Calling PopulateContactPersons");
                _database.Sql().Call("PopulateContactPersons").NonQuery();
                _log.Info("PopulateContactPersons done");
            }
            catch(Exception ex)
            {
                _log.Error("PopulateContactPersons failed", ex);
            }
        }

        public void SyncShadowCustomers()
        {
            _database.Sql().Call("SyncShadowCustomers").WithParam("@projectId", _session.Project.Id).NonQuery();
        }
                
        public CustomerOverview GetOverview(string email)
        {
            return GetOverviews(new[] { email }).FirstOrDefault();
        }

        public IEnumerable<CustomerOverview> GetOverviews(IEnumerable<string> emails)
        {
            var entities = GetCustomerEntities(emails);


            var orders =
                _database.SelectFrom<IPurchaseOrder>()
                    .Join(o => o.Currency)
                    .Where(o => o.ProjectId == _session.Project.Id)
                    .Where(o => o.CustomerEmail.InCsv(entities.Select(e => e.Email)))
                    .Execute()
                    .ToList();

            foreach (var entity in entities)
            {
                var model = new CustomerOverview
                                {
                                    Email = entity.Email,
                                    CustomerId = entity.Id,
                                    Name = entity.Name,
                                    IsDistributor = entity.IsDistributor,
                                    IsNewsletterSubscriber = entity.NewsletterSubscriber,
                                    IsRegistered = entity.IsRegistered,
                                    Nick = entity.Nick
                                };

                foreach (var orderEntity in orders.Where(o => o.CustomerEmail.Equals(entity.Email, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(o => o.PurchaseDate))
                {
                    if (string.IsNullOrWhiteSpace(model.Currency))
                    {
                        model.Currency = orderEntity.Currency?.Symbol;
                    }

                    model.Orders.Add(
                        new CustomerOrderOverview()
                            {
                                PurchaseOrderId = orderEntity.Id,
                                Dt = orderEntity.PurchaseDate,
                                IsCanceled = OrderStatus.IsUnsuccessfullyClosed(orderEntity.OrderStatusId),
                                IsComplete =
                                    (orderEntity.OrderStatusId == OrderStatus.Sent.Id)
                                    || (orderEntity.OrderStatusId == OrderStatus.Packed.Id),
                                CustomerMessage = orderEntity.CustomerNote,
                                InternalMessage = orderEntity.InternalNote,
                                OrderNumber = orderEntity.OrderNumber,
                                Total = orderEntity.PriceWithVat
                            });
                }

                model.Messages.AddRange(entity.Notes);
                model.TotalSpent = model.Orders.Where(o => o.IsComplete).Sum(m => m.Total);

                yield return model;
            }
        }

        private IEnumerable<ICustomer> GetCustomerEntities(IEnumerable<string> emails)
        {
            using (var tx = _database.OpenTransaction())
            {
                var entities = 
                    _database.SelectFrom<ICustomer>()
                        .Join(c => c.Notes)
                        .Where(c => (c.ProjectId == _session.Project.Id) && c.Email.InCsv(emails))
                        .Execute().ToList();


                var unknowns =
                    emails.Where(
                            source =>
                                    !entities.Any(e => e.Email.Equals(source, StringComparison.InvariantCultureIgnoreCase)))
                        .ToList();

                foreach (var unknown in unknowns)
                {
                    _database.Sql()
                        .Call("syncShadowCustomers")
                        .WithParam("@projectId", _session.Project.Id)
                        .WithParam("@email", unknown)
                        .NonQuery();

                    var entity =
                        _database.SelectFrom<ICustomer>()
                            .Join(c => c.Notes)
                            .Where(c => (c.ProjectId == _session.Project.Id) && (c.Email == unknown))
                            .Execute()
                            .FirstOrDefault();

                    if (entity != null)
                    {
                        entities.Add(entity);
                    }
                }

                tx.Commit();

                return entities;
            }
        }

        private void SaveCustomer(ICustomer customer)
        {
            customer.ProjectId = _session.Project.Id;
            _database.Save(customer);
        }

        private void SyncCustomer(IErpCustomerModel src, ICustomer trg, string changeLogGroupingTag)
        {
            var changes = new List<ICustomerChangeLog>();

            void LogChange(string field, object oldVal, object newVal)
            {
                if (trg.Id == 0)
                    return;

                changes.Add(CreateChangeLog(trg.Id, field, oldVal, newVal, changeLogGroupingTag));
            }

            T TrackChange<T>(string name, Func<ICustomer, T> oldValueGetter, Func<IErpCustomerModel, T> newValueGetter, bool allowOverwriteByNull = false, Func<T, T, bool> comparer = null, Action<IErpCustomerModel, ICustomer> customOnChanged = null)
            {
                comparer = comparer ?? new Func<T, T, bool>((a, b) => (a?.Equals(b) == true) || (a == null && b == null));
                
                var oldVal = oldValueGetter(trg);
                var newVal = newValueGetter(src);
                if (newVal == null)
                    return oldVal;

                if (comparer(oldVal, newVal))
                    return newVal;

                LogChange(name, oldVal, newVal);

                customOnChanged?.Invoke(src, trg);

                return newVal;
            }
                        
            if (trg.Id == 0)
            {                
                trg.FirstContactDt = GetFirstContact(src.Email);
            }

            var ename = src.IsCompany ? (src.Name ?? src.Email) : $"{src.Name} {src.Surname}".Trim();
            trg.Name = TrackChange("Jméno", t => t.Name, _ => ename);

            trg.Email = TrackChange("Email", t => t.Email, s => s.Email, comparer: (a, b) => a?.Equals(b, StringComparison.InvariantCultureIgnoreCase) == true, 
                customOnChanged: (s, t) => {
                var changeRecord = _database.New<ICustomerEmailChange>(ch =>
                {
                    ch.ChangeDt = DateTime.Now;
                    ch.OldEmail = trg.Email;
                    ch.NewEmail = src.Email;
                    ch.ErpUid = src.ErpCustomerId;
                    ch.ProjectId = _session.Project.Id;
                });
                _database.Save(changeRecord);
            });

            trg.LastActivationDt = TrackChange(
                "Datum aktivace účtu", 
                t => t.LastActivationDt, 
                s => s.IsActive ? (DateTime?)DateTime.Now : null, 
                comparer: (_, __) => !((trg.LastActivationDt == null) && src.IsActive));

            if ((trg.LastDeactivationDt == null) && (trg.LastActivationDt != null) && (!src.IsActive))
            {
                LogChange("Datum deaktivace účtu", trg.LastDeactivationDt, DateTime.Now); 
                trg.LastDeactivationDt = DateTime.Now;
            }

            trg.Phone = TrackChange("Telefon", t => t.Phone, s => s.Phone);
            
            if (trg.IsRegistered != src.IsRegistered)
            {
                if (src.IsRegistered)
                {
                    LogChange("Datum registrace", trg.RegistrationDt, DateTime.Now);
                    trg.IsRegistered = true;
                    trg.RegistrationDt = DateTime.Now;
                }
                else
                {
                    LogChange("Zrusena registrace", trg.RegistrationDt, DateTime.Now);
                    trg.IsRegistered = false;                    
                }                
            }

            trg.IsDistributor = TrackChange("Velkoodběratel", t => t.IsDistributor, s => s.IsDistributor);
            trg.VatId = TrackChange("DIČ", t => t.VatId, s => s.VatId);           
            trg.CompanyRegistrationId = TrackChange("IČO", t => t.CompanyRegistrationId, s => s.CompanyRegistrationId);
            trg.CompanyName = TrackChange("Název firmy", t => t.CompanyName, s => s.CompanyName);
            trg.Street = TrackChange("Ulice", t => t.Street, s => s.Street);
            trg.DescriptiveNumber = TrackChange("Č.P.", t => t.DescriptiveNumber, s => s.DescriptiveNumber);
            trg.OrientationNumber = TrackChange("Č.O.", t => t.DescriptiveNumber, s => s.DescriptiveNumber);
            trg.City = TrackChange("Město", t => t.City, s => s.City);
            trg.Zip = TrackChange("PSČ", t => t.Zip, s => s.Zip);
            trg.Country = TrackChange("Země", t => t.Country, s => s.Country);
            trg.MainUserEmail = TrackChange("1. Kontaktní osoba - email", t => t.MainUserEmail, s => s.MainUserEmail);
            trg.IsCompany = TrackChange("Firma", t => t.IsCompany, s => s.IsCompany);
            
            if ((trg.DisabledDt != null) != src.IsDisabled) 
            {                
                LogChange("Datum deaktivace v systému", trg.DisabledDt, src.IsDisabled ? DateTime.Now : (DateTime?)null);
                trg.DisabledDt = src.IsDisabled ? DateTime.Now : (DateTime?)null;
            }

            if (trg.NewsletterSubscriber != src.IsNewsletterSubscriber)
            {
                if (trg.NewsletterUnsubscribeDt == null) // If someone unsubscribed in Mailchimp, they cannot subscribe from ERP anymore
                                                         // (bcs currently there is no way how to tell ERP that they unsubscribed from Mailchimp)
                {
                    trg.NewsletterSubscriber = src.IsNewsletterSubscriber;
                    if (src.IsNewsletterSubscriber)
                    {                        
                        LogChange("Datum přihlášení odběru newsletteru", trg.NewsletterSubscriptionDt, DateTime.Now);
                        trg.NewsletterSubscriptionDt = DateTime.Now;
                    }
                    else
                    {                        
                        LogChange("Datum odhlášení odběru newsletteru", trg.NewsletterUnsubscribeDt, DateTime.Now);
                        trg.NewsletterUnsubscribeDt = DateTime.Now;
                    }
                }
            }

            trg.Nick = TrackChange("Zkratka v systému", t => t.Nick, _ => GetNick(trg));
            trg.SearchTag = TrackChange("Search Tag", t => t.SearchTag, s => GetSearchTag(src));
            trg.ErpUid = TrackChange("ERPUID", t => t.ErpUid, s => s.ErpCustomerId);

            var isInsert = trg.Id == 0;
            if (changes.Count > 0 || isInsert)
            {
                trg.LastImportDt = DateTime.Now;
                SaveCustomer(trg);

                if (isInsert)
                    LogChange("Záznam vytvořen", null, DateTime.Now);
            }

            var importedGroups = (src.Groups ?? string.Empty).Split(',').Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct().ToList();
            var existingGroups = _database.SelectFrom<ICustomerGroup>().Where(g => g.CustomerId == trg.Id).Execute().ToList();

            foreach(var importedGroup in importedGroups) 
            {
                if (existingGroups.Any(eg => eg.ErpGroupName.Equals(importedGroup, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                var ng = _database.New<ICustomerGroup>();
                ng.CustomerId = trg.Id;
                ng.ErpGroupName = importedGroup;
                _database.Save(ng);
                existingGroups.Add(ng);
                _log.Info($"Customer {trg.Name} added to group {ng.ErpGroupName}");

                LogChange($"Členem kategorie {ng.ErpGroupName}", false, true);
            }

            foreach(var toDelete in existingGroups) 
            {                
                if (importedGroups.Any(ig => ig.Equals(toDelete.ErpGroupName, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                _database.Delete(toDelete);
                _log.Info($"Customer {trg.Name} removed from group {toDelete.ErpGroupName}");

                LogChange($"Členem kategorie {toDelete.ErpGroupName}", true, false);
            }

            SaveCustomerSalesRep(trg.Id, src.SalesRepresentativeEmail, (old, neue) =>
            {
                LogChange("OZ", old, neue);
            });

            if (changes.Count > 0)
            {
                var key = Guid.NewGuid().ToString();
                foreach(var c in changes)
                {
                    c.GroupingKey = key;
                }

                _database.SaveAll(changes);
            }
        }

        private string GetNick(ICustomer trg)
        {
            if (!string.IsNullOrWhiteSpace(trg.Name))
            {
                var parts = trg.Name.Split((char[])null);
                parts = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

                var nick = string.Empty;
                var slen = Math.Min(1, parts.Length - 1);

                while ((nick.Trim().Length < 3) && (slen <= parts.Length))
                {
                    nick = string.Join(" ", parts.Take(slen));
                    slen++;
                }

                return nick;
            }

            if (!string.IsNullOrWhiteSpace(trg.Email))
            {
                return trg.Email.Split('@', '.').First();
            }

            if (!string.IsNullOrWhiteSpace(trg.Phone))
            {
                return trg.Phone;
            }

            return $"Customer_{trg.Id}";
        }

        private DateTime GetFirstContact(string srcEmail)
        {
            var dt =
                _database.Sql()
                    .ExecuteWithParams(
                        "SELECT MIN(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = {0} AND CustomerEmail = {1}",
                        _session.Project.Id,
                        srcEmail)
                    .Scalar<DateTime?>();

            return dt ?? DateTime.Now;
        }

        private string GetSearchTag(IErpCustomerModel cm)
        {
            return StringUtil.NormalizeSearchText(1000, cm.Name, cm.Email, cm.Surname, cm.Phone);
        }
                
        public List<string> GetSubscribersToSync(string sourceName)
        {
            var sql = @"SELECT cus.Email
                          FROM Customer cus
                         WHERE cus.ProjectId = @projectId
                           AND cus.NewsletterSubscriber = 1
                           AND cus.Email NOT IN (SELECT sub.Email 
                                                   FROM NewsletterSubscriber sub 
						                          WHERE sub.ProjectId = {0}
						                            AND sub.SourceName = {1})";

            var missingSubscribers = _database.Sql().ExecuteWithParams(sql, _session.Project.Id, sourceName).MapRows(r => r.GetString(0));
            return missingSubscribers.ToList();
        }

        public Dictionary<string, ICustomerGroupType> GetCustomerGroupTypes()
        {
            return _database.SelectFrom<ICustomerGroupType>().Where(c => c.ProjectId == _session.Project.Id).Execute().ToDictionary(g => g.ErpGroupName, g => g);
        }

        public Dictionary<int, IAddress> GetDistributorDeliveryAddressesIndex()
        {            
            var result = new Dictionary<int, IAddress>();

            Func<DbDataReader, AddressModel> addressParser = null;
            _database.Sql().Call("GetDeliveryAddressesIndex")
                .WithParam("@projectId", _session.Project.Id)
                .ReadRows(reader => {
                    addressParser = addressParser ?? reader.GetRowParser<AddressModel>(typeof(AddressModel));
                    var parsed = addressParser(reader);

                    result[parsed.CustomerId] = parsed;
                });

            return result;
        }

        public Dictionary<int, string> GetCustomerSalesRepresentativeEmailIndex()
        {
            return _cache.ReadThrough(GetSalesRepIndexCacheKey(), TimeSpan.FromMinutes(10), () =>
            {
                var result = new Dictionary<int, string>();

                _database.Sql().ExecuteWithParams(@"SELECT c.Id CustomerId, sr.NameInErp
                                              FROM Customer c
                                              JOIN SalesRepCustomer src ON (c.Id = src.CustomerId)
                                              JOIN SalesRepresentative   sr ON (src.SalesRepId = sr.Id)
                                             WHERE src.ValidFrom < GETDATE()
                                               AND ((src.ValidTo IS NULL) OR (src.ValidTo > GETDATE()))
                                               AND c.ProjectId = {0}", _session.Project.Id)
                    .ReadRows<int, string>(
                    (cid, email) => result[cid] = email);

                return result;
            });
        }

        public void SaveCustomerSalesRep(int customerId, string salesRepEmail, Action<string, string> onChange)
        {
            if (string.IsNullOrWhiteSpace(salesRepEmail))
                salesRepEmail = null;

            var index = GetCustomerSalesRepresentativeEmailIndex();

            if (index.TryGetValue(customerId, out var existingSrep)) 
            {
                if (existingSrep.Equals(salesRepEmail, StringComparison.InvariantCultureIgnoreCase))
                    return;
            }
            else if (salesRepEmail == null) 
            {
                return;
            }
                                    
            //SaveCustomerSalesRep(@projectId INT, @userId INT, @customerId INT, @salesRepEmail nvarchar(100))
            var existing = _database.Sql().Call("SaveCustomerSalesRep")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@userId", _session.User.Id)
                .WithParam("@customerId", customerId)
                .WithParam("@salesRepEmail", salesRepEmail)
                .NonQuery();

            _cache.Remove(GetSalesRepIndexCacheKey());

            onChange(existingSrep, salesRepEmail);
        }

        private string GetSalesRepIndexCacheKey() 
        {
            return $"customerSRepIndex_{_session.Project.Id}";
        }

        public void SnoozeCustomer(int customerId)
        {
            var rec = _database.New<IDistributorSnooze>();
            rec.AuthorId = _session.User.Id;
            rec.CustomerId = customerId;
            rec.SetDt = DateTime.Now;

            LogCustomerChange(customerId, "Odložit do další objednávky", null, DateTime.Now);

            _database.Save(rec);
        }

        public ICustomerChangeLog LogCustomerChange(int customerId, string field, object oldValue, object newValue, string groupingKey = null)
        {
            var rec = CreateChangeLog(customerId, field, oldValue, newValue, groupingKey);
            _database.Save(rec);

            return rec;
        }

        private class AddressModel : IAddress
        {
            public int CustomerId { get; set; }

            public int Id { get; set; }

            public string CompanyName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Country { get; set; }
            public string Phone { get; set; }
            public string Note { get; set; }
            public string Lat { get; set; }
            public string Lon { get; set; }
            public string GeoInfo { get; set; }
            public string GeoQuery { get; set; }
            public string Street { get; set; }
            public string DescriptiveNumber { get; set; }
            public string OrientationNumber { get; set; }
            public string City { get; set; }
            public string Zip { get; set; }
        }       

        private ICustomerChangeLog CreateChangeLog(int customerId, string field, object oldValue, object newValue, string groupingKey = null)
        {
            string GetValueString(object val)
            { 
                if (val == null)
                    return string.Empty;

                if (val is bool x)
                    return x ? "ANO" : "NE";

                if (val is DateTime dt)
                    return StringUtil.FormatDateTime(dt);

                return StringUtil.Limit(val.ToString(), 1000, "...");
            };
            
            var record = _database.New<ICustomerChangeLog>();
            record.ChangeDt = DateTime.Now;
            record.AuthorId = _session.User.Id;
            record.CustomerId = customerId;
            record.Field = field;
            record.OldValue = GetValueString(oldValue);
            record.NewValue = GetValueString(newValue);
            record.GroupingKey = groupingKey ?? Guid.NewGuid().ToString();


            return record;
        }

        public List<ICustomerRelatedNote> GetCustomerRelatedNotes(int customerId)
        {
            return _cache.ReadThrough($"customernotes_{customerId}", TimeSpan.FromMinutes(10), () => _database.SelectFrom<ICustomerRelatedNote>()
                .Join(c => c.Customer)
                .Where(c => c.CustomerId == customerId)
                .Where(c => c.Customer.ProjectId == _session.Project.Id)
                .OrderByDesc(n => n.CreateDt)
                .Execute()
                .ToList());
        }

        public void AddCustomerNote(int customerId, string text)
        {
            var customer = _database
                .SelectFrom<ICustomer>()
                .Where(c => c.Id == customerId && c.ProjectId == _session.Project.Id)
                .Take(1)
                .Execute()
                .FirstOrDefault()
                .Ensure("Invalid customerId");

            var note = _database.New<ICustomerRelatedNote>();
            note.AuthorId = _session.User.Id;
            note.CustomerId = customer.Id;
            note.CreateDt = DateTime.Now;
            note.Body = text;

            _database.Save(note);

            _cache.Remove($"customernotes_{customerId}");
        }

        public void DeleteCustomerNote(int noteId)
        {
            var note = _database.SelectFrom<ICustomerRelatedNote>().Where(n => n.Id == noteId && n.AuthorId == _session.User.Id).Execute().FirstOrDefault();

            if (note == null)
                throw new ArgumentException("Poznamku nelze smazat");

            _database.Delete(note);
            _cache.Remove($"customernotes_{note.CustomerId}");
        }

        public ICustomer GetCustomer(int id)
        {
            return _database
                .SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == _session.Project.Id)
                .Where(c => c.Id == id)
                .Execute()
                .FirstOrDefault();
        }

        public Dictionary<int, string> GetDistributorNameIndex()
        {
            return _cache.ReadThrough("distributorNameIndex"
                , TimeSpan.FromHours(1)
                , () => {

                    var result = new Dictionary<int, string>();

                    _database
                    .Sql()
                    .Execute("select c.Id, c.Name from Customer c where c.IsCompany = 1 Or c.IsDistributor = 1")
                    .ReadRows<int, string>((id, name) => result[id] = name);

                    return result;
                });
        }

        public IEnumerable<CustomerChanges> GetCustomerChanges(int customerId)
        {
            var data = _database.SelectFrom<ICustomerChangeLog>()
                .Join(c => c.Author)
                .Where(c => c.CustomerId == customerId)
                .OrderByDesc(c => c.ChangeDt)
                .Execute()
                .ToList();

            var aggregated = new List<CustomerChanges>();

            foreach(var change in data) 
            {
                var agg = aggregated.FirstOrDefault(a => a.Day.Date == change.ChangeDt.Date && a.Author == change.Author?.EMail);

                if (agg == null)
                {
                    agg = new CustomerChanges { Author = change.Author.EMail, CustomerId = change.CustomerId, Day = change.ChangeDt.Date };
                    aggregated.Add(agg);
                }

                agg.AddChange(change.Field, change.OldValue, change.NewValue);
            }

            return aggregated;
        }
    }
}
