using System;
using System.Linq;

using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.CurrencyRates
{
    public class CurrencyConversionHelper : ICurrencyConversionHelper
    {
        private readonly ICurrencyRepository m_currencyRepository;
        private readonly IDatabase m_database;


        public CurrencyConversionHelper(ICurrencyRepository currencyRepository, IDatabase database)
        {
            m_currencyRepository = currencyRepository;
            m_database = database;
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
    }
}
