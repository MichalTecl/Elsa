using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Transactions;
using System.Xml.Linq;

namespace Robowire.RobOrm.SqlServer
{
    public interface IDeadlockDiagnosticSink
    {
        void Write(DeadlockDiagnostic diagnostic);
    }

    public sealed class DeadlockDiagnostic
    {
        public string Id { get; internal set; }
        public DateTime OccurredUtc { get; internal set; }
        public string Server { get; internal set; }
        public string Database { get; internal set; }
        public Guid ClientConnectionId { get; internal set; }
        public int? SessionId { get; internal set; }
        public string CommandText { get; internal set; }
        public string Status { get; internal set; }
        public string Details { get; internal set; }
        public List<string> CandidateGraphs { get; } = new List<string>();
    }

    /// <summary>Optional, best-effort diagnostics. Never retries the application command.</summary>
    public static class DeadlockDiagnostics
    {
        public const string EXCEPTION_DATA_KEY = "RobOrm.DeadlockDiagnosticId";
        private const int QUEUE_CAPACITY = 32;
        private static readonly BlockingCollection<Request> _queue = new BlockingCollection<Request>(QUEUE_CAPACITY);
        private static readonly object _configurationLock = new object();
        private static volatile IDeadlockDiagnosticSink _sink;
        private static readonly HashSet<string> _deniedConnections = new HashSet<string>();
        private static bool _started;

        public static void Configure(IDeadlockDiagnosticSink sink)
        {
            lock (_configurationLock)
            {
                _sink = sink;
                if (_started || sink == null)
                    return;

                // Do not carry the caller's TransactionScope or request context into the worker.
                var thread = new Thread(Work) { IsBackground = true, Name = "RobOrm deadlock diagnostics" };
                if (ExecutionContext.IsFlowSuppressed())
                    thread.Start();
                else
                    using (ExecutionContext.SuppressFlow())
                        thread.Start();
                _started = true;
            }
        }

        internal static void Capture(SqlException exception, SqlCommand command, Func<SqlConnection> connectionFactory)
        {
            try
            {
                var sink = _sink;
                if (sink == null || !exception.Errors.Cast<SqlError>().Any(e => e.Number == 1205))
                    return;

                lock (exception)
                {
                    if (exception.Data.Contains(EXCEPTION_DATA_KEY))
                        return;

                    var diagnostic = new DeadlockDiagnostic
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        OccurredUtc = DateTime.UtcNow,
                        Server = command.Connection.DataSource,
                        Database = command.Connection.Database,
                        ClientConnectionId = exception.ClientConnectionId,
                        CommandText = command.CommandText,
                        Status = "Pending"
                    };
                    // Older System.Data.SqlClient versions do not expose the server session ID.
                    // Failure to extract it leaves the reports explicitly labelled as candidates.
                    var match = System.Text.RegularExpressions.Regex.Match(exception.Message, @"Process ID (\d+)");
                    if (match.Success)
                        diagnostic.SessionId = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                    exception.Data[EXCEPTION_DATA_KEY] = diagnostic.Id;
                    if (!_queue.TryAdd(new Request(diagnostic, sink, connectionFactory)))
                        exception.Data[EXCEPTION_DATA_KEY] = diagnostic.Id + " (diagnostic queue full; graph not collected)";
                }
            }
            catch
            {
                // Diagnostics must never replace the application exception.
            }
        }

        private static void Work()
        {
            foreach (var request in _queue.GetConsumingEnumerable())
            {
                try
                {
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        Save(request);
                        for (var attempt = 0; attempt < 3; attempt++)
                        {
                            Thread.Sleep(attempt == 0 ? 2000 : 5000);
                            ReadGraphs(request);
                            if (request.Diagnostic.CandidateGraphs.Count != 0)
                                break;
                        }
                        request.Diagnostic.Status = request.Diagnostic.CandidateGraphs.Count == 0
                            ? "NotFound" : "Candidates";
                        request.Diagnostic.Details = "Reports filtered by UTC time, database and victim SPID when available; correlation is not guaranteed.";
                    }
                }
                catch (Exception ex)
                {
                    request.Diagnostic.Status = "CollectionFailed";
                    // No connection strings or parameter values are written to diagnostics.
                    request.Diagnostic.Details = ex.Message;
                }
                Save(request);
            }
        }

        private static void Save(Request request)
        {
            try { request.Sink.Write(request.Diagnostic); }
            catch { /* A file or logging failure must not stop the worker. */ }
        }

        private static void ReadGraphs(Request request)
        {
            using (var connection = request.ConnectionFactory())
            {
                var builder = new SqlConnectionStringBuilder(connection.ConnectionString)
                {
                    Enlist = false,
                    ConnectTimeout = 5,
                    InitialCatalog = request.Diagnostic.Database
                };
                connection.ConnectionString = builder.ConnectionString;
                if (_deniedConnections.Contains(builder.ConnectionString))
                    throw new InvalidOperationException("Extended Events access was denied earlier. Collection is disabled for this connection until process restart.");
                connection.Open();
                string filename;
                int databaseId;
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 5;
                    command.CommandText = @"SELECT DB_ID(), x.TargetXml.value('(EventFileTarget/File/@name)[1]', 'nvarchar(4000)')
FROM sys.dm_xe_sessions s
JOIN sys.dm_xe_session_targets t ON t.event_session_address = s.address
CROSS APPLY (SELECT CAST(t.target_data AS xml) AS TargetXml) x
WHERE s.name = 'system_health' AND t.target_name = 'event_file'";
                    using (var reader = ExecuteDiagnosticReader(command, builder.ConnectionString))
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("system_health event_file target is unavailable.");
                        databaseId = reader.GetInt32(0);
                        filename = reader.GetString(1);
                    }
                }
                var separator = Math.Max(filename.LastIndexOf('/'), filename.LastIndexOf('\\'));
                var pattern = filename.Substring(0, separator + 1) + "system_health*.xel";
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 10;
                    command.CommandText = @"SELECT x.EventXml
FROM sys.fn_xe_file_target_read_file(@path, NULL, NULL, NULL)
CROSS APPLY (SELECT CAST(event_data AS xml) AS EventXml) x
WHERE object_name = 'xml_deadlock_report'
AND x.EventXml.value('(event/@timestamp)[1]', 'datetime2') BETWEEN @from AND @to";
                    command.Parameters.AddWithValue("@path", pattern);
                    command.Parameters.AddWithValue("@from", request.Diagnostic.OccurredUtc.AddSeconds(-15));
                    command.Parameters.AddWithValue("@to", request.Diagnostic.OccurredUtc.AddSeconds(5));
                    using (var reader = ExecuteDiagnosticReader(command, builder.ConnectionString))
                    {
                        while (reader.Read())
                        {
                            var eventXml = XElement.Parse(reader.GetSqlXml(0).Value);
                            foreach (var graph in eventXml.Descendants("deadlock"))
                            {
                                if (!Matches(graph, request.Diagnostic.SessionId, databaseId))
                                    continue;
                                var xml = graph.ToString();
                                if (!request.Diagnostic.CandidateGraphs.Contains(xml))
                                    request.Diagnostic.CandidateGraphs.Add(xml);
                            }
                        }
                    }
                }
            }
        }

        private static SqlDataReader ExecuteDiagnosticReader(SqlCommand command, string connectionKey)
        {
            try { return command.ExecuteReader(); }
            catch (SqlException ex)
            {
                if (ex.Errors.Cast<SqlError>().Any(e => e.Number == 229 || e.Number == 297 || e.Number == 300))
                    _deniedConnections.Add(connectionKey);
                throw;
            }
        }

        private static bool Matches(XElement graph, int? sessionId, int databaseId)
        {
            var victims = new HashSet<string>(graph.Descendants("victimProcess").Select(v => (string)v.Attribute("id")));
            return graph.Descendants("process").Any(p =>
                victims.Contains((string)p.Attribute("id")) &&
                (int?)p.Attribute("currentdb") == databaseId &&
                (!sessionId.HasValue || (int?)p.Attribute("spid") == sessionId.Value));
        }

        private sealed class Request
        {
            public Request(DeadlockDiagnostic diagnostic, IDeadlockDiagnosticSink sink, Func<SqlConnection> connectionFactory)
            {
                Diagnostic = diagnostic;
                Sink = sink;
                ConnectionFactory = connectionFactory;
            }
            public DeadlockDiagnostic Diagnostic { get; }
            public IDeadlockDiagnosticSink Sink { get; }
            public Func<SqlConnection> ConnectionFactory { get; }
        }
    }
}
