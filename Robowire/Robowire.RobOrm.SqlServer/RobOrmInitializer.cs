using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Web;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.DefaultRules;
using Robowire.RobOrm.Core.Migration;
using Robowire.RobOrm.Core.Migration.Internal;
using Robowire.RobOrm.Core.Query.Model;
using Robowire.RobOrm.SqlServer.Migration;

namespace Robowire.RobOrm.SqlServer
{
    public static class RobOrmInitializer
    {
        public static Action<IContainer, MigrationCustomizer> InitializeAndGetMigrator(
            IContainerSetup s,
            Func<ISqlConnectionStringProvider> connectionStringProviderFactory,
            params Assembly[] entitiesOrigin)
        {
            s.For<ISqlConnectionStringProvider>().Import.FromFactory(locator => connectionStringProviderFactory());

            s.For<IDataModelHelper>().Use<CachedDataModelHelper>();
            s.For<ITransactionManager<SqlConnection>>().Use<ConnectionCreator>();
            s.For<IDatabase>().Use<Database>();
            s.For<ISchemaMigrator>().Use<SchemaMigrator>();
            s.For<IEntityNamingConvention>().Use<DefaultEntityNamingConvention>();

            foreach (var assembly in entitiesOrigin)
            {
                s.ScanAssembly(assembly);
            }

            Action<IContainer, MigrationCustomizer> migratorFunc = (container, customizer) =>
                {
                    var sqlScriptGenerator = new SqlMigrationScriptBuilder();
                    var hashBuilder = new MigrationHashBuilder();
                    var proxy = new ScriptBuilderProxy(hashBuilder, sqlScriptGenerator);

                    if (customizer != null)
                    {
                        proxy.AddCustomScript(customizer.BeforeMigrationScript, customizer.AfterMigrationScript);
                    }

                    using (var locator = container.GetLocator())
                    {
                        var migrator = locator.Get<ISchemaMigrator>();
                        migrator.MigrateStructure(locator, proxy);

                        var hash = hashBuilder.GetHash();
                        var script = sqlScriptGenerator.ToString(hash);

                        var connectionBuilder = locator.Get<ITransactionManager<SqlConnection>>();

                        using (var connection = connectionBuilder.Open(false))
                        {
                            using (var command = new SqlCommand(script, connection.GetConnection()))
                            {
                                command.CommandTimeout = 1000000;
                                command.ExecuteNonQuery();
                            }

                            connection.Commit();
                        }
                        
                        if (!string.IsNullOrWhiteSpace(customizer.ScriptsRoot))
                        {
                            ScriptsMigrator.RunScripts(connectionBuilder, customizer.ScriptsRoot);
                        }
                    }
                };

            return migratorFunc;
        }
    }
}
