using System;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Logging
{
    [Entity]
    public interface ISysLog
    {
        long Id { get; }

        [NotFk]
        long? SessionId { get; set; }

        DateTime EventDt { get; set; }

        bool IsError { get; set; }

        bool IsStopWatch { get; set; }

        int? MeasuredTime { get; set; }

        [NVarchar(255, false)]
        string Method { get; set; }

        [NVarchar(1000, false)]
        string Message { get; set; }
    }
}
