using System;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionMailingHistory : IIntIdEntity
   {
        [NotFk]
        int IssueId { get; set; }

        DateTime SentDt { get; set; }

        [NVarchar(200, false)]
        string EMail { get; set; }
    }
}
