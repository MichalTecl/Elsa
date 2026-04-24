using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailConversationSummary : IIntIdEntity
    {

        [NVarchar(200, false)]
        string SubjectSummary { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string Summary { get; set; }
    }
}
