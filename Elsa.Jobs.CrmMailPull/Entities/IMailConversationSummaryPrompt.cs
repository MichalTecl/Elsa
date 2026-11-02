using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;
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
