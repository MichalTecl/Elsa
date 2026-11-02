using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Web.Hosting;

using Elsa.Core.Entities.Commerce.Common;

using RobOrm.Core;
using RobOrm.Core.EntityGeneration;
using RobOrm.Core.Query.Model;
using RobOrm.SqlServer;
using RobOrm.SqlServer.Migration;

using Robowire;
using Robowire.Diagnostics;

namespace Elsa.Core.Entities.Commerce
{
    public static class ElsaDbInstaller
    {
        /// <summary>
        /// Sets up RobOrm, registers it to the container and migrates the database schema.
        /// </summary>
        /// <param name="container">Application container</param>
        /// <param name="entityAssemblies">Assemblies containing entities (in addition to this assembly)</param>
        public static void Initialize(IContainer container, IEnumerable<Assembly> entityAssemblies)
        {
            var orm = RobOrmConfiguration.Create()
                .WithLog(StartupLog.Write)
                .ScanAssembly(typeof(IProject).Assembly)
                .ScanAssemblies(entityAssemblies)
                .UseSqlServer(new ConnectionStringProvider());

            container.Setup(
                s =>
                    {
                        // FromFactory instead of Existing: Robowire's Existing requires the instance type to have exactly one public constructor
                        s.For<ISqlConnectionStringProvider>().Import.FromFactory(l => orm.ConnectionStringProvider);
                        s.For<IEntityFactory>().Import.FromFactory(l => orm.EntityFactory);
                        s.For<IEntityCollector>().Import.FromFactory(l => orm.EntityCollector);
                        s.For<IDataModelHelper>().Import.FromFactory(l => orm.DataModel);
                        s.For<IEntityNamingConvention>().Import.FromFactory(l => orm.NamingConvention);
                        s.For<ITransactionManager<SqlConnection>>().Use<ConnectionCreator>();
                        s.For<IDatabase>().Use<Database>();
                    });

            orm.MigrateSchema(new MigrationCustomizer
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
