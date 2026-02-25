using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailConversation : IIntIdEntity
    {
        [NVarchar(100, false)]
        string ConversationUid { get; set; }

        [NVarchar(100, true)]
        string Hint { get; set; }
    }
}
