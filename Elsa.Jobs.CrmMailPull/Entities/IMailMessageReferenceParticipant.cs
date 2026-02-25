using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailMessageReferenceParticipant : IIntIdEntity
    {
        int MailMessageReferenceId { get; set; }
        IMailMessageReference MailMessageReference { get; }

        [NVarchar(100, false)]
        string AddressHash { get; set; }
    }
}
