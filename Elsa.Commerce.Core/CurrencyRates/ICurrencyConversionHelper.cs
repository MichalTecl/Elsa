using System;

using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Commerce.Core.CurrencyRates
{
    public interface ICurrencyConversionHelper
    {
        decimal TryConvertToPrimaryCurrency(string sourceCurrencySymbol,
            decimal sourcePrice,
            Action<ICurrencyConversion> conversionCallback);

        ICurrencyConversion GetConversion(int id);
    }
}
