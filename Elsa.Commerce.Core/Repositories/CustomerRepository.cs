using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Crm.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Crm;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ILog m_log;

        public CustomerRepository(IDatabase database, ISession session, ILog log)
        {
            m_database = database;
            m_session = session;
            m_log = log;
        }

        public void SyncCustomers(IEnumerable<IErpCustomerModel> source)
        {
            var allDbCustomers =
                m_database.SelectFrom<ICustomer>().Where(c => c.ProjectId == m_session.Project.Id).Execute().ToList();

            foreach (var src in source)
            {
                var trg =
                    allDbCustomers.FirstOrDefault(
                        s => s.Email.Equals(src.Email, StringComparison.InvariantCultureIgnoreCase))
                    ?? m_database.New<ICustomer>();

                SyncCustomer(src, trg);
            }
        }

        public void SyncShadowCustomers()
        {
            m_database.Sql().Call("SyncShadowCustomers").WithParam("@projectId", m_session.Project.Id).NonQuery();
        }

        public void PutComment(int customerId, string body)
        {
            throw new NotImplementedException();
        }

        public CustomerOverview GetOverview(string email)
        {
            return GetOverviews(new[] { email }).FirstOrDefault();
        }

        public IEnumerable<CustomerOverview> GetOverviews(IEnumerable<string> emails)
        {
            var entities = GetCustomerEntities(emails);


            var orders =
                m_database.SelectFrom<IPurchaseOrder>()
                    .Join(o => o.Currency)
                    .Where(o => o.ProjectId == m_session.Project.Id)
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
            using (var tx = m_database.OpenTransaction())
            {
                var entities = 
                    m_database.SelectFrom<ICustomer>()
                        .Join(c => c.Notes)
                        .Where(c => (c.ProjectId == m_session.Project.Id) && c.Email.InCsv(emails))
                        .Execute().ToList();


                var unknowns =
                    emails.Where(
                            source =>
                                    !entities.Any(e => e.Email.Equals(source, StringComparison.InvariantCultureIgnoreCase)))
                        .ToList();

                foreach (var unknown in unknowns)
                {
                    m_database.Sql()
                        .Call("syncShadowCustomers")
                        .WithParam("@projectId", m_session.Project.Id)
                        .WithParam("@email", unknown)
                        .NonQuery();

                    var entity =
                        m_database.SelectFrom<ICustomer>()
                            .Join(c => c.Notes)
                            .Where(c => (c.ProjectId == m_session.Project.Id) && (c.Email == unknown))
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
            customer.ProjectId = m_session.Project.Id;
            m_database.Save(customer);
        }

        private void SyncCustomer(IErpCustomerModel src, ICustomer trg)
        {
            var changed = false;

            if (trg.Id == 0)
            {
                changed = true;
                trg.FirstContactDt = GetFirstContact(src.Email);
            }

            if (string.IsNullOrWhiteSpace(trg.Email))
            {
                trg.Email = src.Email;
                changed = true;
            }

            if ((trg.Phone != src.Phone) && !string.IsNullOrWhiteSpace(src.Phone))
            {
                trg.Phone = src.Phone;
                changed = true;
            }

            if (trg.IsDistributor != src.IsDistributor)
            {
                trg.IsDistributor = src.IsDistributor;
                changed = true;
            }

            if ((trg.LastActivationDt == null) && src.IsActive)
            {
                trg.LastActivationDt = DateTime.Now;
                changed = true;
            }

            if ((trg.LastDeactivationDt == null) && (trg.LastActivationDt != null) && (!src.IsActive))
            {
                trg.LastDeactivationDt = DateTime.Now;
                changed = true;
            }

            if (!trg.IsRegistered)
            {
                trg.IsRegistered = true;
                trg.RegistrationDt = DateTime.Now;
                changed = true;
            }

            if (trg.IsDistributor != src.IsDistributor)
            {
                trg.IsDistributor = src.IsDistributor;
                changed = true;
            }

            if (trg.NewsletterSubscriber != src.IsNewsletterSubscriber)
            {
                if (trg.NewsletterUnsubscribeDt == null) // If someone unsubscribed in Mailchimp, they cannot subscribe from ERP anymore
                                                         // (bcs currently there is no way how to tell ERP that they unsubscribed from Mailchimp)
                {
                    trg.NewsletterSubscriber = src.IsNewsletterSubscriber;
                    if (src.IsNewsletterSubscriber)
                    {
                        trg.NewsletterSubscriptionDt = DateTime.Now;
                    }
                    else
                    {
                        trg.NewsletterUnsubscribeDt = DateTime.Now;
                    }

                    changed = true;
                }
            }

            var nick = GetNick(trg);
            if (nick != trg.Nick)
            {
                trg.Nick = nick;
                changed = true;
            }

            var searchTag = GetSearchTag(src);
            if (trg.SearchTag != searchTag)
            {
                trg.SearchTag = searchTag;
                changed = true;
            }

            if (changed)
            {
                trg.LastImportDt = DateTime.Now;
                SaveCustomer(trg);
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
                m_database.Sql()
                    .ExecuteWithParams(
                        "SELECT MIN(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = {0} AND CustomerEmail = {1}",
                        m_session.Project.Id,
                        srcEmail)
                    .Scalar<DateTime?>();

            return dt ?? DateTime.Now;
        }

        private string GetSearchTag(IErpCustomerModel cm)
        {
            return StringUtil.NormalizeSearchText(1000, cm.Name, cm.Email, cm.Surname, cm.Phone);
        }

        public void UpdateNewsletterSubscribersList(string sourceName, Dictionary<string, bool> actualSubscriers)
        {
            var localSubscribers = m_database.SelectFrom<INewsletterSubscriber>().Where(s => s.ProjectId == m_session.Project.Id && s.SourceName == sourceName).Execute().ToList();
            var customers = m_database
                .SelectFrom<ICustomer>().Where(c => c.ProjectId == m_session.Project.Id)
                .Execute()
                .ToList();

            void update(string email, bool status, bool updateCustomerTable) 
            {
                email = email.Trim().ToLowerInvariant();

                var localRecord = localSubscribers.FirstOrDefault(local => local.Email.Equals(email));
                
                if ((localRecord != null) && (status == (localRecord.UnsubscribeDt == null)))
                    return;

                m_log.Info($"Newsletter list member update received: {email} = {(status ? "SUBSCRIBED" : "UNSUBSCRIBED")}");

                localRecord = localRecord ?? m_database.New<INewsletterSubscriber>();
                localRecord.Email = email;
                localRecord.ProjectId = m_session.Project.Id;
                localRecord.SourceName = sourceName;

                if (status)
                {
                    localRecord.UnsubscribeDt = null;
                    localRecord.SubscribeDt = DateTime.Now;
                }
                else
                {
                    localRecord.UnsubscribeDt = DateTime.Now;
                }

                m_database.Save(localRecord);

                if (updateCustomerTable)
                {
                    var localCustomer = customers.FirstOrDefault(lc => lc.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase)) ?? m_database.New<ICustomer>();

                    if (status)
                    {
                        if (localCustomer.NewsletterSubscriber && (localCustomer.NewsletterSubscriptionDt != null))
                            return;
                    }
                    else
                    {
                        if ((!localCustomer.NewsletterSubscriber) && (localCustomer.NewsletterUnsubscribeDt != null))
                            return;
                    }

                    localCustomer.ProjectId = m_session.Project.Id;
                    localCustomer.Email = email;
                    
                    if(localCustomer.Id < 1)
                    {
                        localCustomer.FirstContactDt = DateTime.Now;
                        localCustomer.Nick = GetNick(localCustomer);
                        localCustomer.SearchTag = StringUtil.NormalizeSearchText(1000, email);
                    }

                    localCustomer.NewsletterSubscriber = status;
                    if (status)
                    {                     
                        localCustomer.NewsletterSubscriptionDt = DateTime.Now;
                        localCustomer.NewsletterUnsubscribeDt = null;
                    }
                    else 
                    {                        
                        localCustomer.NewsletterUnsubscribeDt = DateTime.Now;
                    }

                    m_database.Save(localCustomer);
                }
            }

            // 1. save received data to db
            foreach (var actual in actualSubscriers)
            {
                update(actual.Key, actual.Value, true);
            }

            var notReceived = localSubscribers
                .Where(l => l.UnsubscribeDt == null)
                .Select(l => l.Email.Trim().ToLowerInvariant())
                .Where(local => !actualSubscriers.ContainsKey(local)).ToList();
            
            foreach(var nr in notReceived)
            {
                update(nr, false, false);
            }           
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

            var missingSubscribers = m_database.Sql().ExecuteWithParams(sql, m_session.Project.Id, sourceName).MapRows(r => r.GetString(0));
            return missingSubscribers.ToList();
        }
    }
}
