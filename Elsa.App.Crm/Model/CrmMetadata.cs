using Elsa.App.Crm.Entities;
using Elsa.Core.Entities.Commerce.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class CrmMetadata
    {
        public CrmMetadata(List<IMeetingStatus> meetingStatusTypes, List<IMeetingStatusAction> meetingStatusActions, List<IMeetingCategory> meetingCategories, List<ICustomerTagType> customerTagTypes, List<ISalesRepresentative> salesRepresentatives, List<ICustomerGroupType> customerGroupTypes)
        {
            MeetingStatusTypes = meetingStatusTypes;
            MeetingStatusActions = meetingStatusActions;
            MeetingCategories = meetingCategories;
            CustomerTagTypes = customerTagTypes;
            SalesRepresentatives = salesRepresentatives;
            CustomerGroupTypes = customerGroupTypes;
        }

        public List<IMeetingStatus> MeetingStatusTypes { get; }
        public List<IMeetingStatusAction> MeetingStatusActions { get; }
        public List<IMeetingCategory> MeetingCategories { get; }
        public List<ICustomerTagType> CustomerTagTypes { get; }
        public List<ISalesRepresentative> SalesRepresentatives { get; }
        public List<ICustomerGroupType> CustomerGroupTypes { get; }
    }
}
