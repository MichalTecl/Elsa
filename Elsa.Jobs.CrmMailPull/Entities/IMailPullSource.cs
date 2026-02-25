using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailPullSource : IIntIdEntity
    {
        [NVarchar(255, false)]
        string Host { get; set; }
        int Port { get; set; }
        bool UseSsl { get; set; }
        [NVarchar(255, false)]
        string Username { get; set; }
        [NVarchar(255, false)]
        string Password { get; set; }
        bool IsEnabled { get; set; }
        IEnumerable<IMailboxFolder> Folders { get; }
    }
}
