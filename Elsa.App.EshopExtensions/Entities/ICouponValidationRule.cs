using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.EshopExtensions.Entities
{
    [Entity]
    public interface ICouponValidationRule : IIntIdEntity, IHasAuthor
    {
        [NVarchar(256, false)]
        string RuleName { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string RuleJson { get; set; }

        DateTime? ValidFrom { get; set; }
        DateTime? ValidTo { get; set; }
    }
}
