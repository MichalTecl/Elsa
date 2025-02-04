using Elsa.Core.Entities.Commerce.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Common
{
    public interface ICustomerRelatedEntity
    {
        int CustomerId { get; set; }
        ICustomer Customer { get; }
    }
}
