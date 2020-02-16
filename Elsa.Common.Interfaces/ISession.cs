using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Common.Interfaces
{
    public interface ISession
    {
        IUser User { get; }

        IProject Project { get; }

        long? SessionId { get; }

        bool VerifyPassword(string hash, string password, bool isDefault);

        string Release { get; }

        string Culture { get; }

        bool HasUserRight(UserRight right);

        bool HasUserRight(string symbol);

        void EnsureUserRight(UserRight right);
    }
}
