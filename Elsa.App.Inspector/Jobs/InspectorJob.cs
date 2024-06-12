using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.App.Inspector.Database;
using Elsa.App.Inspector.Repo;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Jobs
{
    public class InspectorJob : IExecutableJob
    {
        private readonly IInspectionsRepository _repo;
        private readonly ILog _log;
        private readonly IMailSender _mailSender;
        private readonly IDatabase _database;
        private readonly ISession _session;

        public InspectorJob(IInspectionsRepository repo, ILog log, IMailSender mailSender, IDatabase database, ISession session)
        {
            _repo = repo;
            _log = log;
            _mailSender = mailSender;
            _database = database;
            _session = session;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Starting inspector job");

            var procedures = _repo.GetInspectionProcedures();

            using (var session = _repo.OpenSession())
            {
                foreach (var proc in procedures)
                {
                    _log.Info($"Calling inspector procedure {proc}");

                    _repo.RunInspection(session, proc);
                }

                _log.Info("Closing inspections session");
                _repo.CloseSession(session);
            }

            var mailList = new List<MailListItem>();

            _database.Sql().Call("inspfw_getIssuesToMail")
                .WithParam("@projectId", _session.Project.Id)
                .ReadRows<int, string, string, string>((issueId, message, email, typeName) =>
                    mailList.Add(new MailListItem(issueId, message, email, typeName)));
                        
            foreach (var recipient in mailList.Select(m => m.Email).Distinct())
            {
                if (!recipient.Contains("@"))
                {
                    _log.Info($"InspectorMailing - skipping e-mail notificaton to invalid address '{recipient}'");
                    continue;
                }

                _log.Info($"Starting e-mail notification generation for {recipient}");

                try
                {
                    SendEmailNotification(mailList, recipient);
                    _log.Info("Notificaton sent");
                }
                catch (Exception ex)
                {
                    _log.Error($"Email notification to {recipient} failed", ex);
                }
            }
        }

        private void SendEmailNotification(IEnumerable<MailListItem> mailList, string recipient)
        {
            var issues = mailList.Where(m => m.Email == recipient).OrderBy(m => m.TypeName).ThenBy(m => m.Message)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Dobré ráno,");

            if (issues.Count == 1)
            {
                sb.AppendLine("mám problém, se kterým potřebuji pomoci:");
            }
            else
            {
                sb.AppendLine("Mám tu nějaké problémy, se kterými potřebuji pomoci:");
            }

            using (var tx = _database.OpenTransaction())
            {
                var currentItemType = string.Empty;
                foreach (var item in issues)
                {
                    if (currentItemType != item.TypeName)
                    {
                        currentItemType = item.TypeName;
                        sb.AppendLine();
                        sb.AppendLine(currentItemType);
                    }

                    sb.AppendLine($"\t{item.Message}");

                    var logItem = _database.New<IInspectionMailingHistory>(h =>
                    {
                        h.EMail = recipient;
                        h.IssueId = item.IssueId;
                        h.SentDt = DateTime.Now;
                    });
                    _database.Save(logItem);
                }

                sb.AppendLine().AppendLine("S pozdravem").AppendLine("Elsa");

                _mailSender.Send(recipient, "Inspekce dat objevila problémy...", sb.ToString());

                tx.Commit();
            }
        }

        private class MailListItem
        {
            public MailListItem(int issueId, string message, string email, string typeName)
            {
                IssueId = issueId;
                Message = message;
                Email = email;
                TypeName = typeName;
            }

            public int IssueId { get; }
            public string Message { get; }
            public string Email { get; }
            public string TypeName { get; }
        }
    }
}
