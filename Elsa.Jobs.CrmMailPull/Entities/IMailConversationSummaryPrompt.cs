using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailConversationSummaryPrompt : IIntIdEntity
    {
        [NVarchar(NVarchar.Max, false)]
        string Prompt { get; set; }

        DateTime CreateDt { get; set; }

        DateTime ConfirmDt { get; set; }

        int AuthorId { get; set; }
        IUser Author { get; }
    }
}
