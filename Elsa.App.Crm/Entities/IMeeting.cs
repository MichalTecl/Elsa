using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface IMeeting : IIntIdEntity, ICustomerRelatedEntity, IHasAuthor
    {
        DateTime StartDt { get; set; }
        DateTime EndDt { get; set; }

        IEnumerable<IMeetingParticipant> Participants { get; }

        int MeetingCategoryId { get; set; }
        IMeetingCategory MeetingCategory { get; }

        int MeetingStatusId { get; set; }
        IMeetingStatus MeetingStatus { get; }

        [NVarchar(255, false)]
        string Title { get; set; }

        [NVarchar(255, false)]
        string Text { get; set; }
    }
}
