using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

using Elsa.Core.Entities.Commerce.Common.Logging;

using Robowire.RobOrm.SqlServer;

namespace Elsa.Common.Logging
{
    public static class AsyncLogger
    {
        private static ISqlConnectionStringProvider s_connectionStringProvider = null;

        private static readonly ConcurrentBag<ISysLog> s_queue = new ConcurrentBag<ISysLog>();

        public static void Write(ISysLog entry)
        {
            return;

            s_queue.Add(entry);
        }

        public static void Initialize(ISqlConnectionStringProvider connectionStringProvider)
        {
            s_connectionStringProvider = connectionStringProvider;

            var thr = new Thread(new ThreadStart(WriterBody)) { Name = "AsyncLoggerThread", IsBackground = true };
            thr.Start();
        }

        private static void WriterBody()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(10000);

                    var entries = new List<ISysLog>(1000);
                    
                    ISysLog entry;
                    while (s_queue.TryTake(out entry) && (entries.Count < 1000))
                    {
                        entries.Add(entry);
                    }

                    if (entries.Count > 0)
                    {
                        SaveEntries(entries);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Thread.Sleep(1000);
                }
            }
        }

        private static void SaveEntries(List<ISysLog> entries)
        {
            using (var connection = new SqlConnection(s_connectionStringProvider.ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_InsertLogEntries", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(
                        new SqlParameter()
                            {
                                ParameterName = "@entries",
                                TypeName = "dbo.SysLogEntriesTable",
                                SqlDbType = SqlDbType.Structured,
                                Value = CreateTable(entries)
                            });

                    command.ExecuteNonQuery();
                }
            }
        }

        private static DataTable CreateTable(IEnumerable<ISysLog> entries)
        {
            var table = new DataTable("dbo.SysLogEntriesTable");
            table.Columns.Add("SessionId");
            table.Columns.Add("EventDt");
            table.Columns.Add("IsError");
            table.Columns.Add("IsStopWatch");
            table.Columns.Add("MeasuredTime");
            table.Columns.Add("Method");
            table.Columns.Add("Message");

            foreach (var i in entries)
            {
                table.Rows.Add(
                    new object[]
                            { i.SessionId, i.EventDt, i.IsError, i.IsStopWatch, i.MeasuredTime, i.Method, i.Message });
            }

            return table;
        }
    }
}
