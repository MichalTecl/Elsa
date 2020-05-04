using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Jobs.AutomaticQueries.Database
{
    [Entity]
    public interface IAutomaticQuery : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(300, false)]
        string TitlePattern { get; set; }

        [NVarchar(300, false)]
        string ProcedureName { get; set; }

        [NVarchar(1000, true)]
        string LastTriggerValue { get; set; }

        [NVarchar(300, false)]
        string MailRecipientGroup { get; set; }
        bool ResultToAttachment { get; set; }

        [ForeignKey(nameof(IAutoQueryParameter.QueryId))]
        IEnumerable<IAutoQueryParameter> Parameters { get; }
    }
}
