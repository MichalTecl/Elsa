using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailboxFolder : IIntIdEntity
    {
        int MailPullSourceId { get; set; }
        IMailPullSource MailPullSource { get; }

        [NVarchar(255, false)]
        string FolderFullName { get; set; }

        [NVarchar(255, true)]
        string Name { get; set; }


        long UidValidity { get; set; }

        bool IsEnabled { get; set; }
    }
}
