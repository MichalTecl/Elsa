using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Commerce.Core.Model
{
    public class BatchPriceInfo
    {
        internal BatchPriceInfo(decimal primaryCurrencyPrice, decimal? sourceCurrencyPrice, ICurrencyConversion conversion)
        {
            PrimaryCurrencyPrice = primaryCurrencyPrice;
            SourceCurrencyPrice = sourceCurrencyPrice;
            Conversion = conversion;
        }

        public decimal PrimaryCurrencyPrice { get; }

        public decimal? SourceCurrencyPrice { get; }

        public ICurrencyConversion Conversion { get; }
    }
}
