using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public interface IErpCustomerModel
    {
        string ErpCustomerId { get; }

        string Email { get; }

        string Phone { get; }

        string Name { get; }

        string Surname { get; }

        bool IsActive { get; }

        bool IsNewsletterSubscriber { get; }

        bool IsDistributor { get; }
    }
}
