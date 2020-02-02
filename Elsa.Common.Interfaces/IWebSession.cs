using System.Web.Routing;

namespace Elsa.Common.Interfaces
{
    public interface IWebSession : ISession
    {
        void Initialize(RequestContext context);

        void Login(string user, string password);

        void Logout();
    }
}
