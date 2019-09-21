using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core.CurrencyConversions;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        private readonly IPerProjectDbCache m_cache;
        

        public CurrencyRepository(IDatabase database, ISession session, IPerProjectDbCache cache)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
        }

        public ICurrency GetCurrency(string symbol)
        {
            return
                GetAllCurrencies()
                    .FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase));
        }

        public ICurrency GetCurrency(int id)
        {
            return GetAllCurrencies().FirstOrDefault(c => c.Id == id);
        }

        public void SaveCurrency(ICurrency currency)
        {
            try
            {
                currency.ProjectId = m_session.Project.Id;

                m_database.Save(currency);
            }
            finally
            {
                m_cache.Remove("currencies");
            }
        }

        public IEnumerable<ICurrency> GetAllCurrencies()
        {
            return m_cache.ReadThrough("currencies", db => db.SelectFrom<ICurrency>());
        }

        public IEnumerable<ICurrencyRate> GetActualCurrencyRates()
        {
            return m_cache.ReadThrough("currates", db => db.SelectFrom<ICurrencyRate>().Join(r => r.SourceCurrency).Join(r => r.TargetCurrency).Where(i => i.ValidTo == null));
        }

        public ICurrencyRate GetCurrencyRate(ICurrency sourceCurrency, ICurrency targetCurrency, DateTime? forDate = null)
        {
            if (forDate == null)
            {
                return
                    GetActualCurrencyRates()
                        .FirstOrDefault(
                            r => (r.SourceCurrencyId == sourceCurrency.Id) && (r.TargetCurrencyId == targetCurrency.Id));
            }

            return
                m_database.SelectFrom<ICurrencyRate>()
                    .Where(r => r.ProjectId == m_session.Project.Id)
                    .Where(c => (c.SourceCurrencyId == sourceCurrency.Id) && (c.TargetCurrencyId == targetCurrency.Id))
                    .Where(r => (r.ValidFrom <= forDate.Value) && (r.ValidTo >= forDate.Value))
                    .Execute()
                    .FirstOrDefault();
        }

        public void SetCurrencyRate(
            ICurrency sourceCurrency,
            ICurrency targetCurrency,
            decimal rate,
            DateTime validFrom,
            string sourceLink)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var oldRate = GetCurrencyRate(sourceCurrency, targetCurrency);

                    if ((oldRate != null) && (oldRate.ValidFrom >= validFrom))
                    {
                        tx.Commit();
                        return;
                    }

                    if (oldRate != null)
                    {
                        oldRate.ValidTo = validFrom;
                        m_database.Save(oldRate);
                    }

                    var actualRate = m_database.New<ICurrencyRate>();
                    actualRate.SourceCurrencyId = sourceCurrency.Id;
                    actualRate.TargetCurrencyId = targetCurrency.Id;
                    actualRate.ValidFrom = validFrom;
                    actualRate.Rate = rate;
                    actualRate.SourceLink = sourceLink;
                    actualRate.ProjectId = m_session.Project.Id;
                    
                    m_database.Save(actualRate);

                    tx.Commit();
                }
            }
            finally
            {
                m_cache.Remove("currates");
            }
        }

        public ICurrencyConversion CreateCurrencyConversion(ICurrencyRate usedRate, decimal sourceValue)
        {
            var converted = sourceValue * usedRate.Rate;

            var conversion = m_database.New<ICurrencyConversion>(c =>
            {
                c.ConversionDt = DateTime.Now;
                c.CurrencyRateId = usedRate.Id;
                c.SourceCurrencyId = usedRate.SourceCurrencyId;
                c.TargetCurrencyId = usedRate.TargetCurrencyId;
                c.SourceValue = sourceValue;
                c.TargetValue = converted;
                c.ConversionDt = DateTime.Now;
                c.ProjectId = usedRate.ProjectId;
            });

            m_database.Save(conversion);

            return conversion;
        }

        public ICurrencyConversion GetCurrencyConversion(int id)
        {
            return m_cache.ReadThrough($"currency_conversion_{id}",
                db => db.SelectFrom<ICurrencyConversion>().Join(c => c.CurrencyRate).Join(c => c.SourceCurrency)
                    .Join(c => c.TargetCurrency).Where(c => c.Id == id)).FirstOrDefault();
        }
    }
}
