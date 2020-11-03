using System;
using System.Web.Hosting;
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
                BeforeMigrationScript = "IF EXISTS(SELECT * FROM sys.columns WHERE name = 'CalculatedKey') BEGIN DROP INDEX MaterialBatch.INX_MaterialBatch_CalculatedKey; ALTER TABLE MaterialBatch DROP COLUMN CalculatedKey;  END",
                AfterMigrationScript = @"ALTER TABLE MaterialBatch ADD CalculatedKey AS BatchNumber + ':' + CAST(MaterialId AS NVARCHAR) PERSISTED;
                                         IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_CalculatedKey')
                                         BEGIN
	                                        CREATE INDEX INX_MaterialBatch_CalculatedKey	ON MaterialBatch	(CalculatedKey);  
                                         END",
                ScriptsRoot = HostingEnvironment.MapPath("/SQL")
            });
        }
    }
}
