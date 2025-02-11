using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface IMeetingParticipant : IIntIdEntity
    {
        int MeetingId { get; set; }
        IMeeting Meeting { get; }

        int ParticipantId { get; set; }
        IUser Participant { get; }
    }
}
