using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Core.CurrencyConversions
{
    [Entity]
    public interface ICurrencyRate : IProjectRelatedEntity
    {
        int Id { get; }

        int SourceCurrencyId { get; set; }
        ICurrency SourceCurrency { get; }

        int TargetCurrencyId { get; set; }
        ICurrency TargetCurrency { get; }

        DateTime ValidFrom { get; set; }

        DateTime? ValidTo { get; set; }

        decimal Rate { get; set; }

        [NVarchar(256, false)]
        string SourceLink { get; set; }
    }
}
