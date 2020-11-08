using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface ILogReaderScanHistory : IIntIdEntity, IProjectRelatedEntity
    {
        DateTime CheckDt { get; set; }
    }
}
