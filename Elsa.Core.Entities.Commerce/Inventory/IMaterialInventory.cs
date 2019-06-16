using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialInventory : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(255, false)]
        string Name { get; set; }

        bool IsManufactured { get; set; }

        bool? RequireInvoicesDefault { get; set; }

        bool? RequirePriceDefault { get; set; } 

        bool? IncludesFixedCosts { get; set; }

        bool CanBeConnectedToTag { get; set; }

        int? AllowedUnitId { get; set; }
        IMaterialUnit AllowedUnit { get; }

        [NVarchar(300, true)]
        string ReceivingInvoiceFormGeneratorName { get; set; }
    }
}
