using System;

using Elsa.Core.Entities.Commerce.Common;

using Robowire;
using Robowire.RobOrm.SqlServer;
using Robowire.RobOrm.SqlServer.Migration;

namespace Elsa.Core.Entities.Commerce
{
    public static class ElsaDbInstaller
    {
        public static void Initialize(IContainer container)
        {
            Action<IContainer, MigrationCustomizer> migrator = null;

            container.Setup(
                s =>
                    {
                        migrator = RobOrmInitializer.InitializeAndGetMigrator(
                            s,
                            () => new ConnectionStringProvider(),
                            typeof(IProject).Assembly);
                    });

            migrator(container, new MigrationCustomizer
            {
                BeforeMigrationScript = "IF EXISTS(SELECT * FROM sys.columns WHERE name = 'CalculatedKey') ALTER TABLE MaterialBatch DROP COLUMN CalculatedKey;",
                AfterMigrationScript = "ALTER TABLE MaterialBatch ADD CalculatedKey AS BatchNumber + ':' + CAST(MaterialId AS NVARCHAR) PERSISTED;"
            });
        }
    }
}
