using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailMessageReference : IIntIdEntity
    {
        int MailboxFolderId { get; set; }
        IMailboxFolder MailboxFolder { get; }
        long ImapUid { get; set; }
        DateTime InternalDt { get; set; }
        int? ConversationId { get; set; }
        IMailConversation Conversation { get; }
        int? FullContentId { get; set; }
        IMailMessageFullContent FullContent { get; }

        [NVarchar(1000, true)]
        string ExclusionRule { get; set; }
    }
}
