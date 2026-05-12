using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Migration
{
    internal static class SchemaMigrationProceduresInstaller
    {
        private const string RESOURCE_NAME = "Robowire.RobOrm.SqlServer.Migration.RobOrmSchemaMigrationProcedures.sql";

        public static void EnsureInstalled(ITransactionManager<SqlConnection> connectionBuilder)
        {
            var script = LoadScript();
            Console.WriteLine("[RobOrm] Ensuring schema migration procedures are installed");

            using (var connection = connectionBuilder.Open(false))
            {
                var sqlConnection = connection.GetConnection();
                SqlConsoleLogging.Attach(sqlConnection, "procedure-install");

                foreach (var batch in SqlBatchReader.GetBatches(script))
                {
                    Console.WriteLine("[RobOrm] Executing embedded schema migration procedures batch");
                    ExecuteBatch(sqlConnection, batch);
                }

                connection.Commit();
            }
        }

        private static string LoadScript()
        {
            var assembly = typeof(SchemaMigrationProceduresInstaller).Assembly;
            using (var stream = assembly.GetManifestResourceStream(RESOURCE_NAME))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Embedded SQL resource '{RESOURCE_NAME}' was not found");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void ExecuteBatch(SqlConnection connection, string batch)
        {
            using (var command = new SqlCommand(batch, connection))
            {
                command.CommandTimeout = 1000000;
                command.ExecuteNonQuery();
            }
        }
    }
}
