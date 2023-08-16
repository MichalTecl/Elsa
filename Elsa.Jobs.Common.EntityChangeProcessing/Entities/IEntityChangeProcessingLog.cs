using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Entities
{
    [Entity]
    public interface IEntityChangeProcessingLog
    {
        long Id { get; }

        [NVarchar(300, false)]
        string ProcessorName { get; set; }

        [NotFk]
        long EntityId { get; set; }

        [NVarchar(300, false)]
        string EntityHash { get; set; }

        [NotFk]
        [NVarchar(300, true)]
        string ExternalId { get; set; }

        DateTime ProcessedDt { get; set; }

        [NVarchar(NVarchar.Max, true)]
        string CustomData { get; set; }
    }
}
