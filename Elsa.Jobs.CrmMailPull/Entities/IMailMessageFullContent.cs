using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailMessageFullContent : IIntIdEntity
    {

        [NVarchar(NVarchar.Max, false)]
        string Content { get; set; }

        [NVarchar(1000, false)]
        string Subject { get; set; }

        [NVarchar(1000, false)]
        string ConversationUid { get; set; }

        [NVarchar(1000, false)]
        string MessageUid { get; set; }

        [NVarchar(1000, false)]
        string Sender { get; set; }
    }
}
