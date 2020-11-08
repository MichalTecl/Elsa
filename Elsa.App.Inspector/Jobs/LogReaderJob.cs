using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Elsa.App.Inspector.Database;
using Elsa.Common.Interfaces;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Jobs
{
    public class LogReaderJob : IExecutableJob
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IMailSender m_mailSender;

        public LogReaderJob(IDatabase database, ISession session, IMailSender mailSender)
        {
            m_database = database;
            m_session = session;
            m_mailSender = mailSender;
        }

        public void Run(string customDataJson)
        {
            var logsFolder = @"C:\Elsa\Log\Jobs";

            var logFiles = Directory.GetFiles(logsFolder, "*.log").OrderByDescending(l => l).Take(3);

            var lastCheckDt = m_database.SelectFrom<ILogReaderScanHistory>()
                .Where(l => l.ProjectId == m_session.Project.Id)
                .OrderByDesc(l => l.CheckDt)
                .Take(1).Execute()
                .FirstOrDefault()?.CheckDt ?? DateTime.Now.AddDays(-2);

            var entries = Read(logFiles).Where(e => e.Severity == "ERR" && e.Dt > lastCheckDt).OrderBy(e => e.Dt).ToList();

            if (!entries.Any())
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Chyby z logu {lastCheckDt:dd/MM HH:mm} - {DateTime.Now:dd/MM HH:mm}:").AppendLine();

            foreach (var entry in entries)
            {
                sb.Append(entry.Dt.ToString("dd/MM HH:mm:ss")).Append("\t").Append(entry.Message);
            }

            m_mailSender.Send("mtecl.prg@gmail.com", "ELSA Chyby z logu", sb.ToString());

            var checkEntry = m_database.New<ILogReaderScanHistory>();
            checkEntry.ProjectId = m_session.Project.Id;
            checkEntry.CheckDt = DateTime.Now;
            
            m_database.Save(checkEntry);
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
