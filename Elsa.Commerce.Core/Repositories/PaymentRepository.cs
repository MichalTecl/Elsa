using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly List<IPayment> m_cache = new List<IPayment>();

        public PaymentRepository(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public void PreloadCache(DateTime from, DateTime to)
        {
            m_cache.Clear();
            m_cache.AddRange(GetPaymentsQuery().Where(p => p.PaymentDt >= from && p.PaymentDt <= to).Execute());
        }

        public IPayment GetPayment(int paymentSourceId, string transactionId)
        {
            var payment = m_cache.FirstOrDefault(p => p.TransactionId == transactionId)
                       ?? GetPaymentsQuery().Where(p => p.PaymentSourceId == paymentSourceId && p.TransactionId == transactionId).Execute().FirstOrDefault();

            return payment;
        }

        public IPayment SavePayment(IPayment payment)
        {
            try
            {
                IPayment fullPayment;

                payment.ProjectId = m_session.Project.Id;

                var toSave = GetPayment(payment.PaymentSourceId, payment.TransactionId);

                if (toSave == null)
                { 
                    m_database.Save(payment);

                    fullPayment = GetPaymentsQuery().Where(p => p.Id == payment.Id).Execute().First();

                    var cachedPayment = m_cache.FirstOrDefault(p => p.Id == payment.Id);
                    if (cachedPayment != null)
                    {
                        m_cache.Remove(cachedPayment);
                    }

                    m_cache.Add(fullPayment);
                }
                else
                {
                    fullPayment = GetPaymentsQuery().Where(p => p.Id == payment.Id).Execute().First();
                }

                return fullPayment;
            }
            catch
            {
                m_cache.Clear();
                throw;
            }
        }

        public IEnumerable<LastPaymentInfo> GetLastPaymentDates()
        {
            const string sql = @"select ps.Id, x.LastPayment
                                  from PaymentSource ps
                                  left join (SELECT p.PaymentSourceId SourceId, MAX(p.PaymentDt) LastPayment
                                               FROM Payment p
			                                  WHERE p.ProjectId = @p
			                                 GROUP BY p.PaymentSourceId
			                                 ) x ON (x.SourceId = ps.Id)
                                WHERE ps.ProjectId = @p";

            var payments = m_database.Sql().Execute(sql).WithParam("@p", m_session.Project.Id).MapRows(
                row =>
                    {
                        DateTime lPayment = DateTime.MinValue;
                        if (!row.IsDBNull(1))
                        {
                            lPayment = row.GetDateTime(1);
                        }

                        return new LastPaymentInfo(row.GetInt32(0), lPayment);
                    });

            return payments;
        }

        private IQueryBuilder<IPayment> GetPaymentsQuery()
        {
            return
                m_database.SelectFrom<IPayment>()
                    .Join(p => p.Currency)
                    .Join(p => p.PaymentSource)
                    .Where(p => p.ProjectId == m_session.Project.Id);
        }
    }
}
