using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.OrderDataValidation.Validations.OrderNote.Entities
{
    [Entity]
    public interface IKitNoteValidationResult : IIntIdEntity, IOrderRelatedEntity
    {
        DateTime ValidationDt { get; set; }

        [NVarchar(255, false)]
        string OrderHash { get; set; }

        [NVarchar(255, false)]
        string CustomerNoteHash { get; set; }

        [NVarchar(1024, true)]
        string ValidatonMessage { get; set; }

        bool IsValid { get; set; }
    }
}
