using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface IPerson : IIntIdEntity
    {
        [NotFk]
        [NVarchar(100, true)]
        string ExternalId { get; set; }

        [NVarchar(100, true)]
        string Email { get; set; }

        [NVarchar(100, true)]
        string Name { get; set; }

        [NVarchar(250, true)]
        string Address { get; set; }

        [NVarchar(100, true)]
        string Phone { get; set; }

        [NVarchar(NVarchar.Max, true)]
        string Note { get; set; }
    }
}
