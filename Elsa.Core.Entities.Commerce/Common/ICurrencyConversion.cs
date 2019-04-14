using System;

using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Core.CurrencyConversions;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface ICurrencyConversion : IProjectRelatedEntity
    {
        int Id { get; }

        int SourceCurrencyId { get; set; }
        ICurrency SourceCurrency { get; }

        int TargetCurrencyId { get; set; }
        ICurrency TargetCurrency { get; }

        DateTime ConversionDt { get; set; }

        int CurrencyRateId { get; set; }

        ICurrencyRate CurrencyRate { get; }

        decimal SourceValue { get; set; }

        decimal TargetValue { get; set; }
    }
}
