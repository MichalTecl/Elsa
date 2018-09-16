using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUserUserRight
    {
        long Id { get; }

        int UserId { get; set; }

        IUser User { get; }

        int UserRightId { get; set; }

        IUserRight UserRight { get; }
    }
}
