using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Common
{
    public interface ISession
    {
        IUser User { get; }

        IProject Project { get; }

        bool VerifyPassword(string hash, string password, bool isDefault);
    }
}
