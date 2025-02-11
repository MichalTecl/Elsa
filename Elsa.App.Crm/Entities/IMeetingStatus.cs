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
    public interface IMeetingStatus : IIntIdEntity, IHasAuthor
    {
        int MeetingId { get; set; }
        IMeeting Meeting { get; }

        int StatusTypeId { get; set; }
        IMeetingStatusType StatusType { get; }

        DateTime SetDt { get; set; }

        [NVarchar(1000, true)]
        string Note { get; set; }
    }
}
