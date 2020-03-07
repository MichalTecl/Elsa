using Elsa.Users.Components;
using Robowire;

namespace Elsa.Users
{
    public class UserRightsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IUserRepository>().Use<UserRepository>();
            setup.For<IUserRoleRepository>().Use<UserRepository>();
        }
    }

    
}
