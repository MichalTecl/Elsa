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
                trg.NewsletterSubscriber = src.IsNewsletterSubscriber;
                if (src.IsNewsletterSubscriber)
                {
                    trg.NewsletterSubscriptionDt = DateTime.Now;
                }

                changed = true;
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

        public void UpdateNewsletterSubscribersList(string sourceName, List<string> actualSubscriers)
        {
            var currentList = m_database.SelectFrom<INewsletterSubscriber>().Where(s => s.ProjectId == m_session.Project.Id && s.SourceName == sourceName).Execute().ToList();

            // 1. delete removed
            foreach(var aliveInDb in currentList.Where(i => i.UnsubscribeDt == null))
            {
                if (actualSubscriers.Contains(aliveInDb.Email, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                m_log.Info($"{aliveInDb.Email} was not recevied in subscribers list - ending subscription in the DB");

                aliveInDb.UnsubscribeDt = DateTime.Now;
                m_database.Save(aliveInDb);
            }

            // 2. add missing
            foreach(var newSubscriber in actualSubscriers)
            {
                var localRecord = currentList.FirstOrDefault(s => s.Email.Equals(newSubscriber, StringComparison.InvariantCultureIgnoreCase));

                if (localRecord == null || localRecord.UnsubscribeDt != null)
                {
                    m_log.Info($"{newSubscriber} was not found in local db - saving");
                    localRecord = localRecord ?? m_database.New<INewsletterSubscriber>();
                    localRecord.Email = newSubscriber;
                    localRecord.ProjectId = m_session.Project.Id;
                    localRecord.SourceName = sourceName;
                    localRecord.UnsubscribeDt = null;
                    localRecord.SubscribeDt = DateTime.Now;
                    m_database.Save(localRecord);
                }
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
