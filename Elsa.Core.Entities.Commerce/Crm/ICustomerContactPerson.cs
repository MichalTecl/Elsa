using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerContactPerson : IIntIdEntity
    {
        int CustomerId { get; set; }
        ICustomer Customer { get; }
        int PersonId { get; set; }
        IPerson Person { get; }
    }
}
