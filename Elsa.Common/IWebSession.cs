using System.Web.Routing;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Common
{
    public interface IWebSession : ISession
    {
        void Initialize(RequestContext context);

        void Login(string user, string password);

        void Logout();
    }
}
