using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface IEntityComment : IIntIdEntity, IProjectRelatedEntity, IHasAuthor
    {
        [NVarchar(255, false)]
        string EntityType { get; set; }

        [NotFk]
        int RecordId { get; set; }

        DateTime WriteDt { get; set; }
        DateTime? DeleteDt { get; set; }

        int? ReplacedCommentId { get; set; }
        IEntityComment ReplacedComment { get; }

        [NVarchar(NVarchar.Max, true)]
        string Text { get; set; }        
    }
}
