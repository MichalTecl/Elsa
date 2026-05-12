using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Migration
{
    internal class ScriptsMigrator
    {
        private const string HISTORY_QUERY = @"SELECT rsl.ScriptName, rsl.FileHash
  FROM Roborm_Scripts_Log rsl
  JOIN (SELECT s.ScriptName, MAX(s.RunDt) LastRun
           FROM Roborm_Scripts_Log s
		 GROUP BY s.ScriptName) x ON (x.ScriptName = rsl.ScriptName AND rsl.RunDt = x.LastRun);";

        public static void RunScripts(ITransactionManager<SqlConnection> sql, string scriptsFolder)
        {
            if (!Directory.Exists(scriptsFolder))
            {
                Console.WriteLine($"[RobOrm] SQL scripts folder '{scriptsFolder}' does not exist");
                return;
            }

            Console.WriteLine($"[RobOrm] Checking SQL scripts in '{scriptsFolder}'");

            using (var conn = sql.OpenUnmanagedConnection())
            {
                conn.Open();
                SqlConsoleLogging.Attach(conn, "extra-scripts-bootstrap");

                EnsureScriptTable(conn);
            }

            var lastHashes = GetLastHashes(sql);

            foreach (var file in Directory.GetFiles(scriptsFolder, "*.sql").Select(Path.GetFileName).OrderBy(f => f))
            {
                if (RunScript(Path.Combine(scriptsFolder, file), lastHashes, sql))
                {
                    Console.WriteLine($"[RobOrm] Script '{file}' changed and was executed");
                    lastHashes.Clear();
                }
                else
                {
                    Console.WriteLine($"[RobOrm] Script '{file}' unchanged");
                }
            }
        }

        private static bool RunScript(string scriptPath, Dictionary<string, string> lastHashes, ITransactionManager<SqlConnection> sql)
        {
            var scriptFileName = Path.GetFileName(scriptPath);

            using (var fileContent = File.OpenRead(scriptPath))
            {
                using (var sha = new SHA256Managed())
                {
                    var hash = BitConverter.ToString(sha.ComputeHash(fileContent)).Replace("-", string.Empty);
                    
                    if (lastHashes.TryGetValue(scriptFileName, out var oldHash) && hash.Equals(oldHash))
                    {
                        return false;
                    }

                    fileContent.Position = 0;


                    using (var reader = new StreamReader(fileContent, Encoding.UTF8))
                    {
                        using (var conn = sql.OpenUnmanagedConnection())
                        {
                            conn.Open();
                            SqlConsoleLogging.Attach(conn, $"extra-script:{scriptFileName}");
                            Console.WriteLine($"[RobOrm] Executing SQL script '{scriptFileName}'");

                            foreach (var partialScript in GetScripts(reader))
                            {
                                using (var cmd = new SqlCommand(partialScript, conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            using (var log =
                                new SqlCommand("INSERT INTO [Roborm_Scripts_Log] VALUES (@script, @hash, GETDATE())",
                                    conn))
                            {
                                log.Parameters.AddWithValue("@script", scriptFileName);
                                log.Parameters.AddWithValue("@hash", hash);

                                log.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void EnsureScriptTable(SqlConnection sqlConnection)
        {
            using (var cmd =
                new SqlCommand(
                    "IF NOT EXISTS(SELECT TOP 1 1 FROM sys.tables WHERE name = N'Roborm_Scripts_Log\')\r\nCREATE TABLE Roborm_Scripts_Log(ScriptName NVARCHAR(1000), FileHash VARCHAR(100), RunDt DATETIME);",
                    sqlConnection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static Dictionary<string, string> GetLastHashes(ITransactionManager<SqlConnection> tm)
        {
            using (var connection = tm.OpenUnmanagedConnection())
            {
                connection.Open();
                using (var cmd = new SqlCommand(HISTORY_QUERY, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        var result = new Dictionary<string, string>();
                        while (reader.Read())
                        {
                            result[reader.GetString(0)] = reader.GetString(1);
                        }
                        return result;
                    }
                }
            }
        }

        private static IEnumerable<string> GetScripts(StreamReader scriptFile)
        {
            return SqlBatchReader.GetBatches(scriptFile);
        }
    }
}
