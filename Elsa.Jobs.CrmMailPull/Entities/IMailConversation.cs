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
        [NVarchar(1000, false)]
        string ConversationUid { get; set; }

        [NVarchar(1000, true)]
        string Hint { get; set; }

        int? SummaryId { get; set; }
        IMailConversationSummary Summary { get; }

        DateTime ConversationEndDt { get; set; }
    }
}
