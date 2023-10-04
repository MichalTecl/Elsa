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

        bool IsNewsletterSubscriber { get; set; }

        bool IsDistributor { get; }

        string Groups { get; }

        string VatId { get; }

        string CompanyRegistrationId { get; }

        string CompanyName { get; }
        string Street { get; }
        string DescriptiveNumber { get; }
        string OrientationNumber { get; }
        string City { get; }
        string Zip { get; }
        string Country { get; }
        string MainUserEmail { get; }
        bool IsCompany { get; }

        bool IsDisabled { get; }

        string SalesRepresentativeEmail { get; }
    }
}
