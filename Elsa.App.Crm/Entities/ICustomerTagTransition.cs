using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ICustomerTagTransition : IIntIdEntity, IHasAuthor
    {
        int SourceTagTypeId { get; set; }
        ICustomerTagType SourceTagType { get; }

        int TargetTagTypeId { get; set; }
        ICustomerTagType TargetTagType { get; }
    }
}
