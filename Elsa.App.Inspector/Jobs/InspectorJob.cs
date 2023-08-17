using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.App.Inspector.Database;
using Elsa.App.Inspector.Repo;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Jobs
{
    public class InspectorJob : IExecutableJob
    {
        private readonly IInspectionsRepository m_repo;
        private readonly ILog m_log;
        private readonly IMailSender m_mailSender;
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public InspectorJob(IInspectionsRepository repo, ILog log, IMailSender mailSender, IDatabase database, ISession session)
        {
            m_repo = repo;
            m_log = log;
            m_mailSender = mailSender;
            m_database = database;
            m_session = session;
        }

        public void Run(string customDataJson)
        {
            m_log.Info("Starting inspector job");

            var procedures = m_repo.GetInspectionProcedures();

            using (var session = m_repo.OpenSession())
            {
                foreach (var proc in procedures)
                {
                    m_log.Info($"Calling inspector procedure {proc}");

                    m_repo.RunInspection(session, proc);
                }

                m_log.Info("Closing inspections session");
                m_repo.CloseSession(session);
            }

            var mailList = new List<MailListItem>();

            m_database.Sql().Call("inspfw_getIssuesToMail")
                .WithParam("@projectId", m_session.Project.Id)
                .ReadRows<int, string, string, string>((issueId, message, email, typeName) =>
                    mailList.Add(new MailListItem(issueId, message, email, typeName)));

            if (AppEnvironment.IsDev) 
            {
                m_log.Info("DEV environment - removing all recipients except mtecl.prg@gmail.com");

                var itemToKeep = mailList.FirstOrDefault(m => m.Email == "mtecl.prg@gmail.com");
                mailList.Clear();

                if (itemToKeep != null)
                    mailList.Add(itemToKeep);
            }

            foreach (var recipient in mailList.Select(m => m.Email).Distinct())
            {
                if (!recipient.Contains("@"))
                {
                    m_log.Info($"InspectorMailing - skipping e-mail notificaton to invalid address '{recipient}'");
                    continue;
                }

                m_log.Info($"Starting e-mail notification generation for {recipient}");

                try
                {
                    SendEmailNotification(mailList, recipient);
                    m_log.Info("Notificaton sent");
                }
                catch (Exception ex)
                {
                    m_log.Error($"Email notification to {recipient} failed", ex);
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

            using (var tx = m_database.OpenTransaction())
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

                    var logItem = m_database.New<IInspectionMailingHistory>(h =>
                    {
                        h.EMail = recipient;
                        h.IssueId = item.IssueId;
                        h.SentDt = DateTime.Now;
                    });
                    m_database.Save(logItem);
                }

                sb.AppendLine().AppendLine("S pozdravem").AppendLine("Elsa");

                m_mailSender.Send(recipient, "Inspekce dat objevila problémy...", sb.ToString());

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
