using System;
using System.Data.SqlClient;

using RobOrm.Core;
using RobOrm.Core.DefaultRules;
using RobOrm.Core.EntityGeneration;
using RobOrm.Core.Migration;
using RobOrm.Core.Migration.Internal;
using RobOrm.Core.Query.Model;
using RobOrm.SqlServer.Migration;

namespace RobOrm.SqlServer
{
    /// <summary>
    /// RobOrm configured for SQL Server. Holds the shared (singleton) parts of RobOrm and creates the per-scope parts.
    /// It doesn't depend on any DI container - register its members to the container of the application.
    /// </summary>
    public sealed class RobOrmSqlServer
    {
        private readonly Action<string> _log;

        public RobOrmSqlServer(EntityRegistry entities, ISqlConnectionStringProvider connectionStringProvider, Action<string> log = null)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (connectionStringProvider == null)
            {
                throw new ArgumentNullException(nameof(connectionStringProvider));
            }

            Entities = entities;
            ConnectionStringProvider = connectionStringProvider;
            _log = log ?? (message => { });
        }

        public EntityRegistry Entities { get; }

        public IEntityFactory EntityFactory => Entities;

        public IEntityCollector EntityCollector => Entities;

        /// <summary>
        /// Thread safe, can be shared.
        /// </summary>
        public IDataModelHelper DataModel { get; } = new CachedDataModelHelper();

        public IEntityNamingConvention NamingConvention { get; } = new DefaultEntityNamingConvention();

        public ISqlConnectionStringProvider ConnectionStringProvider { get; }

        /// <summary>
        /// The transaction manager keeps the current transaction - create one per scope (request, job run...).
        /// </summary>
        public ITransactionManager<SqlConnection> CreateTransactionManager()
        {
            return new ConnectionCreator(ConnectionStringProvider);
        }

        public IDatabase CreateDatabase(ITransactionManager<SqlConnection> transactionManager)
        {
            return new Database(EntityFactory, DataModel, transactionManager);
        }

        public ISchemaMigrator CreateSchemaMigrator()
        {
            return new SchemaMigrator(DataModel, NamingConvention, EntityCollector);
        }

        /// <summary>
        /// Creates/updates the database schema according to the registered entities. The migration script runs only when
        /// the schema hash differs from the one stored in the database.
        /// </summary>
        public void MigrateSchema(MigrationCustomizer customizer)
        {
            _log("RobOrm: schema migration started");
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

            CreateSchemaMigrator().MigrateStructure(proxy);

            var hash = hashBuilder.GetHash();
            var script = RenderMigrationScript(sqlScriptGenerator, hash);
            _log("RobOrm: schema hash computed");
            Console.WriteLine($"[RobOrm] Computed schema hash: {hash}");

            var connectionBuilder = CreateTransactionManager();

            var versionMatches = false;
            try
            {
                using (var connection = connectionBuilder.Open(false))
                {
                    var sqlConnection = connection.GetConnection();
                    SqlConsoleLogging.Attach(sqlConnection, "version-check");

                    using (var command = new SqlCommand("SELECT TOP 1 SchemaHash FROM Roborm_Schema_Version ORDER BY ApplyDt DESC", sqlConnection))
                    {
                        var dbHash = command.ExecuteScalar();
                        versionMatches = dbHash?.ToString() == hash;
                    }

                    connection.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[RobOrm] Schema version lookup failed, migration will continue");
                Console.WriteLine(ex.ToString());
            }

            _log("RobOrm: schema version checked");

            if (!versionMatches)
            {
                Console.WriteLine("[RobOrm] Schema hash differs, applying schema migration script");

                // The procedures are used only by the migration script, so they are (re)installed only when the migration runs
                if (ProceduralMigrationFeatures.UseStoredProcedureSchemaMigration)
                {
                    SchemaMigrationProceduresInstaller.EnsureInstalled(connectionBuilder);
                    _log("RobOrm: migration procedures installed");
                }

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

                _log("RobOrm: schema migration script applied");
            }
            else
            {
                Console.WriteLine("[RobOrm] Schema hash matches, skipping schema migration");
                _log("RobOrm: schema migration skipped (hash matches)");
            }

            if (!string.IsNullOrWhiteSpace(customizer?.ScriptsRoot))
            {
                Console.WriteLine($"[RobOrm] Running additional SQL scripts from '{customizer.ScriptsRoot}'");
                ScriptsMigrator.RunScripts(connectionBuilder, customizer.ScriptsRoot);
                _log("RobOrm: additional SQL scripts done");
            }

            Console.WriteLine("[RobOrm] Schema migration finished");
            _log("RobOrm: schema migration finished");
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
    }

    public static class RobOrmSqlServerConfigurationExtensions
    {
        /// <summary>
        /// Generates the registered entities and returns RobOrm configured for SQL Server.
        /// </summary>
        public static RobOrmSqlServer UseSqlServer(this RobOrmConfiguration configuration, ISqlConnectionStringProvider connectionStringProvider)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new RobOrmSqlServer(configuration.BuildEntityRegistry(), connectionStringProvider, configuration.Log);
        }
    }
}
