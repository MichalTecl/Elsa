using System;

using Elsa.Core.Entities.Commerce.Common;

using Robowire;
using Robowire.RobOrm.SqlServer;

namespace Elsa.Core.Entities.Commerce
{
    public static class ElsaDbInstaller
    {
        public static void Initialize(IContainer container)
        {
            Action<IContainer> migrator = null;

            container.Setup(
                s =>
                    {
                        migrator = RobOrmInitializer.InitializeAndGetMigrator(
                            s,
                            () => new ConnectionStringProvider(),
                            typeof(IProject).Assembly);
                    });

            migrator(container);
        }
    }
}
