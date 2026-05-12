using System;
using System.Data.SqlClient;

namespace Robowire.RobOrm.SqlServer.Migration
{
    internal static class SqlConsoleLogging
    {
        public static void Attach(SqlConnection connection, string scope)
        {
            connection.InfoMessage += (sender, args) =>
            {
                foreach (SqlError error in args.Errors)
                {
                    Console.WriteLine($"[SQL:{scope}] {error.Message}");
                }
            };
        }
    }
}
