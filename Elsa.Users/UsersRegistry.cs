using Elsa.Users.Components;
using Elsa.Users.SyncJob;
using Robowire;

namespace Elsa.Users
{
    public class UserRightsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IUserRepository>().Use<UserRepository>();
            setup.For<IUserRoleRepository>().Use<UserRepository>();
            setup.For<IUserManagementFacade>().Use<UserManagementFacade>();

            setup.For<SyncUserRightTypesStartupJob>().Use<SyncUserRightTypesStartupJob>();            
        }
    }
}
