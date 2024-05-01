using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomer : IProjectRelatedEntity, IPostalAddress
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

        DateTime? NewsletterUnsubscribeDt { get; set; }

        bool IsDistributor { get; set; }

        bool IsRegistered { get; set; }

        DateTime? RegistrationDt { get; set; }

        IEnumerable<ICustomerRelatedNote> Notes { get; }

        [NVarchar(100, true)]
        string ErpUid { get; set; }

        [NotFk]
        [NVarchar(100, true)]
        string VatId { get; set; }

        [NotFk]
        [NVarchar(100, true)]
        string CompanyRegistrationId { get; set; }

        [NVarchar(128, true)]
        string CompanyName { get; set; }

        [NVarchar(128, true)]
        string Country { get; set; }

        [NVarchar(255, true)]
        string MainUserEmail { get; set; }
                
        bool? IsCompany { get; set; }
        
        DateTime? DisabledDt { get; set; }

        bool? HasEshop { get; set; }
        bool? HasStore { get; set; }

        /// <summary>
        /// Means that the customer for example made 2 or more orders in the eshop
        /// </summary>
        bool? IsValuableDistributor { get; set; }
        DateTime? IsValuableDistributorChangeDt { get; set; }
    }
}
