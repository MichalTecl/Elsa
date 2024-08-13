using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface IDistributorSnooze : IIntIdEntity
    {
        int CustomerId { get; set; }
        ICustomer Customer { get; }
        DateTime SetDt { get; set; }
        int AuthorId { get; set; }
        IUser Author { get; }
    }
}
