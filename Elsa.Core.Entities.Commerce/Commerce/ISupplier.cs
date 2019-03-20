using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface ISupplier : IProjectRelatedEntity, IChangeControlEntity
    {
        int Id { get; }

        [NVarchar(250, false)]
        string Name { get; set; }

        [NVarchar(250, false)]
        string Street { get; set; }

        [NVarchar(250, false)]
        string City { get; set; }

        [NVarchar(5, false)]
        string Country { get; set; }

        [NVarchar(250, false)]
        string Zip { get; set; }

        [NVarchar(250, false)]
        string IdentificationNumber { get; set; }

        [NVarchar(250, false)]
        string TaxIdentificationNumber { get; set; }

        [NVarchar(20, true)]
        string ContactPhone { get; set; }

        [NVarchar(64, true)]
        string ContactEmail { get; set; }

        [NVarchar(1000, true)]
        string Note { get; set; }

        int CurrencyId { get; set; }
        ICurrency Currency { get; }
    }
}
