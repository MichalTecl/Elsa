using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Smtp.Core.Database
{
    [Entity]
    public interface IEmailRecipientList : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(100, false)]
        string GroupName { get; set; }

        [NVarchar(1000, false)]
        string Addresses { get; set; }
    }
}
