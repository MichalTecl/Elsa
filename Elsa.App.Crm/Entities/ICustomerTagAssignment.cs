using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ICustomerTagAssignment : IIntIdEntity, ICustomerRelatedEntity, IHasAuthor
    {
        int TagTypeId { get; set; }
        ICustomerTagType TagType { get; }
        DateTime AssignDt { get; set; }

        DateTime? UnassignDt { get; set; }

        [NVarchar(2000, true)]
        string Note { get; set; }
    }
}
