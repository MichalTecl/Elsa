using System;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface IBaseCalendar
    {
        int Id { get; }

        DateTime Day { get; set; }

        bool IsBusinessDay { get; set; }
    }
}
