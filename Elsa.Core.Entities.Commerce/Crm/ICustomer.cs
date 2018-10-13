using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomer : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(255, true)]
        string Email { get; set; }
        
        [NVarchar(255, true)]
        string Phone { get; set; }

        [NVarchar(255, true)]
        string Name { get; set; }

        [NVarchar(255, false)]
        string Nick { get; set; }

        [NVarchar(1000, false)]
        string SearchTag { get; set; }

        DateTime FirstContactDt { get; set; }

        DateTime? LastImportDt { get; set; }

        DateTime? LastActivationDt { get; set; }

        DateTime? LastDeactivationDt { get; set; }

        bool NewsletterSubscriber { get; set; }

        DateTime? NewsletterSubscriptionDt { get; set; }

        bool IsDistributor { get; set; }

        bool IsRegistered { get; set; }

        DateTime? RegistrationDt { get; set; }

        IEnumerable<ICustomerRelatedNote> Notes { get; }
    }
}
