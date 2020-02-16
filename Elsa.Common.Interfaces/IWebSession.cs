using System.Web.Routing;

namespace Elsa.Common.Interfaces
{
    public interface IWebSession : ISession
    {
        string[] UserRights { get; }

        void Initialize(RequestContext context);

        void Login(string user, string password);

        void Logout();
    }
}
