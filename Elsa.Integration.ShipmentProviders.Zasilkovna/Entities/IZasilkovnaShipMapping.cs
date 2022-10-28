using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna.Entities
{
    [Entity]
    public interface IZasilkovnaShipMapping : IIntIdEntity
    {
        [NotFk]
        [NVarchar(100, false)]
        string ZasilkovnaId { get; set; }
        
        [NVarchar(1000, false)]
        string Name { get; set; }
    }
}
