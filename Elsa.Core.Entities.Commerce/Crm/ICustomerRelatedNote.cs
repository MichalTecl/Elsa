using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerRelatedNote : IHasAuthor
    {
        int Id { get; }
                
        int CustomerId { get; set; }

        ICustomer Customer { get; }

        DateTime CreateDt { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string Body { get; set; }
    }
}
