using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Core.CurrencyConversions
{
    [Entity]
    public interface IMoneyConversion : IProjectRelatedEntity
    {
        int Id { get; }

        decimal SourceAmount { get; set; }
        decimal ConvertedAmount { get; set; }

        int RateId { get; set; }
        ICurrencyRate Rate { get; }
    }
}
