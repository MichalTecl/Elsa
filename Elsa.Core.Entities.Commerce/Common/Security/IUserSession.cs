using System;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUserSession
    {
        long Id { get; }
        
        Guid PublicId { get; set; }

        DateTime StartDt { get; set; }

        DateTime? EndDt { get; set; }

        int? ProjectId { get; set; }

        int? UserId { get; set; }

        IProject Project { get; }

        IUser User { get; }
    }
}
