using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core.CurrencyConversions;

namespace Elsa.Commerce.Core
{
    public interface ICurrencyRepository
    {
        ICurrency GetCurrency(string symbol);

        void SaveCurrency(ICurrency currency);

        IEnumerable<ICurrency> GetAllCurrencies();

        IEnumerable<ICurrencyRate> GetActualCurrencyRates();

        ICurrencyRate GetCurrencyRate(ICurrency sourceCurrency, ICurrency targetCurrency, DateTime? forDate = null);

        void SetCurrencyRate(
            ICurrency sourceCurrency,
            ICurrency targetCurrency,
            decimal rate,
            DateTime validFrom,
            string sourceLink);

        ICurrencyConversion CreateCurrencyConversion(ICurrencyRate usedRate, decimal sourceValue);

        ICurrencyConversion GetCurrencyConversion(int id);
    }
}
