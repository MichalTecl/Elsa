using System;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;

using RobOrm.Core;

namespace Elsa.Commerce.Core.CurrencyRates
{
    public class CurrencyConversionHelper : ICurrencyConversionHelper
    {
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;

        public CurrencyConversionHelper(ICurrencyRepository currencyRepository, IDatabase database, ICache cache, ISession session)
        {
            _currencyRepository = currencyRepository;
            _database = database;
            _cache = cache;
            _session = session;
        }

        public decimal TryConvertToPrimaryCurrency(string sourceCurrencySymbol, decimal sourcePrice, Action<ICurrencyConversion> conversionCallback)
        {
            using (var tx = _database.OpenTransaction())
            {
                var sourceCurrency = _currencyRepository.GetCurrency(sourceCurrencySymbol);
                if (sourceCurrency == null)
                {
                    throw new InvalidOperationException($"Neznamy symbol meny '{sourceCurrencySymbol}'");
                }

                if (sourceCurrency.IsProjectMainCurrency)
                {
                    tx.Commit();
                    return sourcePrice;
                }

                var targetCurrency = _currencyRepository.GetAllCurrencies()
                    .FirstOrDefault(c => c.IsProjectMainCurrency);
                if (targetCurrency == null)
                {
                    throw new InvalidOperationException("Není nastavena domácí měna projektu");
                }

                var rate = _currencyRepository.GetCurrencyRate(sourceCurrency, targetCurrency);
                if (rate == null)
                {
                    throw new InvalidOperationException($"Není dostupná konverze {sourceCurrency.Symbol} -> {targetCurrency.Symbol}");
                }

                var conversion = _currencyRepository.CreateCurrencyConversion(rate, sourcePrice);
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
            return _cache.ReadThrough($"currency_conversion_{id}",
                TimeSpan.FromMinutes(1),
                () => _database.SelectFrom<ICurrencyConversion>().Join(c => c.CurrencyRate).Join(c => c.SourceCurrency)
                    .Join(c => c.TargetCurrency).Where(c => c.ProjectId == _session.Project.Id).Where(c => c.Id == id)
                    .Take(1).Execute().FirstOrDefault());
        }
    }
}
