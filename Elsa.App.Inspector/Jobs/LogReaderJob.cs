using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Elsa.App.Inspector.Database;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Jobs
{
    public class LogReaderJob : IExecutableJob
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IMailSender _mailSender;
        private readonly ILog _log;

        public LogReaderJob(IDatabase database, ISession session, IMailSender mailSender, ILog log)
        {
            _database = database;
            _session = session;
            _mailSender = mailSender;
            _log = log;
        }

        public void Run(string customDataJson)
        {
            var logsFolder = @"C:\Elsa\Log\Jobs";

            _log.Info($"Reading log files from {logsFolder}");

            var logFiles = Directory.GetFiles(logsFolder, "*.log").OrderByDescending(l => l).Take(3).ToList();

            _log.Info($"Found {logFiles.Count} newest log files: {string.Join(",", logFiles.Select(l => Path.GetFileName(l)))} ");

            var lastCheckDt = _database.SelectFrom<ILogReaderScanHistory>()
                .Where(l => l.ProjectId == _session.Project.Id)
                .OrderByDesc(l => l.CheckDt)
                .Take(1).Execute()
                .FirstOrDefault()?.CheckDt ?? DateTime.Now.AddDays(-31);

            _log.Info($"LastCheckDt = {lastCheckDt}");

            var rawEntries = Read(logFiles).ToList();
            _log.Info($"Log files contain {rawEntries.Count} of total entries");
            
            rawEntries = rawEntries.Where(e => e.Dt > lastCheckDt).ToList();
            _log.Info($"Log files contain {rawEntries.Count} of entries logged after last check");

            ProcessInspectionIssues(rawEntries);

            var entries = rawEntries.Where(e => e.Severity == "ERR").OrderBy(e => e.Dt).ToList();
            _log.Info($"{entries.Count} of ER entries");

            if (entries.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Chyby z logu {lastCheckDt:dd/MM HH:mm} - {DateTime.Now:dd/MM HH:mm} ({entries.Count} záznamů):").AppendLine();

                foreach (var entry in entries)
                {
                    sb.Append(entry.Dt.ToString("dd/MM HH:mm:ss")).Append("\t").Append(entry.Message).AppendLine();
                }

                _log.Info($"Mail message composed, sending");

                _mailSender.Send(SenderMailboxType.SystemRobot, "mtecl.prg@gmail.com", "ELSA Chyby z logu", sb.ToString());

                _log.Info($"Mail message sent");
            }
            else
            {
                _log.Info($"No ER entries");
            }

            var checkEntry = _database.New<ILogReaderScanHistory>();
            checkEntry.ProjectId = _session.Project.Id;
            checkEntry.CheckDt = DateTime.Now;
            
            _database.Save(checkEntry);

            _log.Info($"LastCheckDt set to {checkEntry.CheckDt}");
        }

        private void ProcessInspectionIssues(List<LogEntryModel> rawEntries)
        {
            var pattern = $"\t{InspectionIssueModel.DataEntryMarker}";

            _log.Info($"Starting processing logged inspection issues (Marked by {InspectionIssueModel.DataEntryMarker})");
            var issues = rawEntries.Where(e => e.Message.Contains(pattern)).ToList();
            _log.Info($"Found {issues.Count} of logged inspection issues");

            if (issues.Count == 0)
            {
                return;
            }

            foreach(var i in issues)
            {
                var message = i.Message.Split('\t').FirstOrDefault(part => part.StartsWith(InspectionIssueModel.DataEntryMarker));
                if (string.IsNullOrWhiteSpace(message))
                {
                    _log.Error($"Failed to extract inspection issue data from message: {i.Message}");
                    continue;
                }

                var data = InspectionIssueModel.Deserialize(message);

                var entry = _database.New<ILogStoredInspectionIssue>();
                entry.ProjectId = _session.Project.Id;
                entry.Message = data.Message;
                entry.IssueCode = data.IssueCode;
                entry.IssueTypeName = data.IssueTypeName;
                entry.LogDt = i.Dt;

                _database.Save(entry);
            }
        }

        private static IEnumerable<LogEntryModel> Read(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var datePart = Path.GetFileNameWithoutExtension(file);
                if (!DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fileDate))
                {
                    continue;
                }

                var lines = File.ReadAllText(file).Split((char)1);
                
                foreach (var line in lines)
                {
                    var splitted = line.Split("\t".ToArray(), 3).Select(s => s.Trim()).ToArray();
                    if (splitted.Length != 3)
                    {
                        continue;
                    }

                    if (!DateTime.TryParseExact(splitted[0], "HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var entryTime))
                    {
                        continue;
                    }

                    yield return new LogEntryModel(
                        new DateTime(fileDate.Year, fileDate.Month, fileDate.Day, entryTime.Hour, entryTime.Minute,
                            entryTime.Second), splitted[1], splitted[2]);
                }
            }
        }

        private sealed class LogEntryModel
        {
            public readonly DateTime Dt;
            public readonly string Severity;
            public readonly string Message;

            public LogEntryModel(DateTime dt, string severity, string message)
            {
                Dt = dt;
                Severity = severity;
                Message = message;
            }
        }
    }
}
