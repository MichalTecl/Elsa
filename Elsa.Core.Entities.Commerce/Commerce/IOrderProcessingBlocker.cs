using Elsa.Core.Entities.Commerce.Common;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IOrderProcessingBlocker : IOrderRelatedEntity, IIntIdEntity, IHasAuthor
    {        
        DateTime CreateDt { get; set; }

        [NVarchar(512, false)]
        string Message { get; set; }

        [NVarchar(255, false)]
        string DisabledStageSymbol { get; set; }
    }    
}
