using System;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.CurrencyRates
{
    public class CurrencyConversionHelper : ICurrencyConversionHelper
    {
        private readonly ICurrencyRepository m_currencyRepository;
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public CurrencyConversionHelper(ICurrencyRepository currencyRepository, IDatabase database, ICache cache, ISession session)
        {
            m_currencyRepository = currencyRepository;
            m_database = database;
            m_cache = cache;
            m_session = session;
        }

        public decimal TryConvertToPrimaryCurrency(string sourceCurrencySymbol, decimal sourcePrice, Action<ICurrencyConversion> conversionCallback)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var sourceCurrency = m_currencyRepository.GetCurrency(sourceCurrencySymbol);
                if (sourceCurrency == null)
                {
                    throw new InvalidOperationException($"Neznamy symbol meny '{sourceCurrencySymbol}'");
                }

                if (sourceCurrency.IsProjectMainCurrency)
                {
                    tx.Commit();
                    return sourcePrice;
                }

                var targetCurrency = m_currencyRepository.GetAllCurrencies()
                    .FirstOrDefault(c => c.IsProjectMainCurrency);
                if (targetCurrency == null)
                {
                    throw new InvalidOperationException("Není nastavena domácí měna projektu");
                }

                var rate = m_currencyRepository.GetCurrencyRate(sourceCurrency, targetCurrency);
                if (rate == null)
                {
                    throw new InvalidOperationException($"Není dostupná konverze {sourceCurrency.Symbol} -> {targetCurrency.Symbol}");
                }

                var conversion = m_currencyRepository.CreateCurrencyConversion(rate, sourcePrice);
                if (conversion == null)
                {
                    throw new InvalidOperationException("Konverze měny selhala");
                }

                conversionCallback(conversion);

                tx.Commit();

                return conversion.TargetValue;
            }
        }

        public ICurrencyConversion GetConversion(int id)
        {
            return m_cache.ReadThrough($"currency_conversion_{id}",
                TimeSpan.FromMinutes(1),
                () => m_database.SelectFrom<ICurrencyConversion>().Join(c => c.CurrencyRate).Join(c => c.SourceCurrency)
                    .Join(c => c.TargetCurrency).Where(c => c.ProjectId == m_session.Project.Id).Where(c => c.Id == id)
                    .Take(1).Execute().FirstOrDefault());
        }
    }
}
