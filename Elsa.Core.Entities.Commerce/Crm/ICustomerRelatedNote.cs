using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerRelatedNote
    {
        int Id { get; }
        
        int AuthorId { get; set; }

        IUser Author { get; }

        int CustomerId { get; set; }

        ICustomer Customer { get; }

        DateTime CreateDt { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string Body { get; set; }
    }
}
