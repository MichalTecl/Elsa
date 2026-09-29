using System;
using System.IO;
using System.Text;
using System.Xml;
using Robowire.RobOrm.SqlServer;

namespace Elsa.Common.Logging
{
    /// <summary>Stores the full report outside SysLog's limited message column.</summary>
    public sealed class DeadlockDiagnosticFileSink : IDeadlockDiagnosticSink
    {
        public const string DIRECTORY = @"C:\Elsa\Log\Deadlocks";

        public void Write(DeadlockDiagnostic diagnostic)
        {
            Directory.CreateDirectory(DIRECTORY);
            for (var i = 0; i < diagnostic.CandidateGraphs.Count; i++)
            {
                File.WriteAllText(Path.Combine(DIRECTORY, diagnostic.Id + "-candidate-" + (i + 1) + ".xdl"),
                    diagnostic.CandidateGraphs[i], Encoding.UTF8);
            }

            var path = Path.Combine(DIRECTORY, diagnostic.Id + ".xml");
            var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartElement("DeadlockDiagnostic");
                writer.WriteElementString("Id", diagnostic.Id);
                writer.WriteElementString("OccurredUtc", diagnostic.OccurredUtc.ToString("O"));
                writer.WriteElementString("Server", diagnostic.Server);
                writer.WriteElementString("Database", diagnostic.Database);
                writer.WriteElementString("ClientConnectionId", diagnostic.ClientConnectionId.ToString());
                writer.WriteElementString("SessionId", diagnostic.SessionId?.ToString());
                writer.WriteElementString("CommandText", diagnostic.CommandText);
                writer.WriteElementString("Status", diagnostic.Status);
                writer.WriteElementString("Details", diagnostic.Details);
                writer.WriteElementString("CandidateCount", diagnostic.CandidateGraphs.Count.ToString());
                writer.WriteEndElement();
            }
        }
    }
}
