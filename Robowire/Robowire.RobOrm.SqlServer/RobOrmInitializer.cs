using System;
using System.Data.SqlClient;
using System.IO;
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
                    Console.WriteLine($"[RobOrm] Starting schema migration. Mode={(ProceduralMigrationFeatures.UseStoredProcedureSchemaMigration ? "procedural" : "legacy")}");

                    IMigrationScriptBuilder sqlScriptGenerator = ProceduralMigrationFeatures.UseStoredProcedureSchemaMigration
                        ? (IMigrationScriptBuilder)new StoredProcedureMigrationScriptBuilder()
                        : new SqlMigrationScriptBuilder();
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
                        var script = RenderMigrationScript(sqlScriptGenerator, hash);
                        Console.WriteLine($"[RobOrm] Computed schema hash: {hash}");
                        TryPersistMigrationScript(script);

                        var connectionBuilder = locator.Get<ITransactionManager<SqlConnection>>();

                        if (ProceduralMigrationFeatures.UseStoredProcedureSchemaMigration)
                        {
                            SchemaMigrationProceduresInstaller.EnsureInstalled(connectionBuilder);
                        }

                        bool versionMatches = false;
                        try
                        {
                            using (var connection = connectionBuilder.Open(false))
                            {
                                var sqlConnection = connection.GetConnection();
                                SqlConsoleLogging.Attach(sqlConnection, "version-check");

                                using (var command = new SqlCommand("SELECT TOP 1 SchemaHash FROM Roborm_Schema_Version ORDER BY ApplyDt DESC", sqlConnection))
                                {
                                    var dbHash = command.ExecuteScalar();
                                    versionMatches = (dbHash?.ToString() == hash);
                                }

                                connection.Commit();
                            }                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[RobOrm] Schema version lookup failed, migration will continue");
                            Console.WriteLine(ex.ToString());                            
                        }

                        if (!versionMatches)
                        {
                            Console.WriteLine("[RobOrm] Schema hash differs, applying schema migration script");
                            using (var connection = connectionBuilder.Open(false))
                            {
                                var sqlConnection = connection.GetConnection();
                                SqlConsoleLogging.Attach(sqlConnection, "schema-migration");

                                using (var command = new SqlCommand(script, sqlConnection))
                                {
                                    command.CommandTimeout = 1000000;
                                    command.ExecuteNonQuery();
                                }

                                connection.Commit();
                            }
                        }
                        else
                        {
                            Console.WriteLine("[RobOrm] Schema hash matches, skipping schema migration");
                        }
                        
                        if (!string.IsNullOrWhiteSpace(customizer.ScriptsRoot))
                        {
                            Console.WriteLine($"[RobOrm] Running additional SQL scripts from '{customizer.ScriptsRoot}'");
                            ScriptsMigrator.RunScripts(connectionBuilder, customizer.ScriptsRoot);
                        }

                        Console.WriteLine("[RobOrm] Schema migration finished");
                    }
                };

            return migratorFunc;
        }

        private static string RenderMigrationScript(IMigrationScriptBuilder sqlScriptGenerator, string hash)
        {
            if (sqlScriptGenerator is SqlMigrationScriptBuilder legacyBuilder)
            {
                return legacyBuilder.ToString(hash);
            }

            if (sqlScriptGenerator is StoredProcedureMigrationScriptBuilder proceduralBuilder)
            {
                return proceduralBuilder.ToString(hash);
            }

            throw new NotSupportedException($"Unsupported migration script builder type: {sqlScriptGenerator?.GetType().FullName}");
        }

        private static void TryPersistMigrationScript(string script)
        {
            try
            {
                // TODO
                return;
                var path = "dbmigration.sql";
                File.WriteAllText(path, script ?? string.Empty);
                Console.WriteLine($"[RobOrm] Migration script saved to '{path}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RobOrm] Failed to save dbmigration.sql: {ex.Message}");
            }
        }

        
    }
}
