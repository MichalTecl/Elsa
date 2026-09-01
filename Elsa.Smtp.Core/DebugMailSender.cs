using Elsa.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Elsa.Smtp.Core
{
    public class DebugMailSender : IMailSender
    {
        private readonly ILog _log;
        private readonly IMailTemplateRenderer _mailTemplateRenderer;

        public DebugMailSender(ILog log, IMailTemplateRenderer mailTemplateRenderer)
        {
            _log = log;
            _mailTemplateRenderer = mailTemplateRenderer;
        }

        public void Send(SenderMailboxType mailbox, string to, string subject, string body, params string[] attachmentFiles)
        {
            SendToGroup(mailbox, to, subject, body, attachmentFiles);
        }

        public void SendToGroup(SenderMailboxType mailbox, string groupName, string subject, string body, params string[] attachmentFiles)
        {            
            string directoryPath = @"C:\Elsa\Log\MailSender";
            Directory.CreateDirectory(directoryPath);

            var uniqueIdentifier = Guid.NewGuid().ToString();
            var fileName = $"{mailbox.TypeName}_{subject}_{uniqueIdentifier}.txt";
            
            var attachments = attachmentFiles.Length > 0 ? "\n\nATTACHMENTS: " + string.Join(", ", attachmentFiles) : string.Empty;

            var fileContents = $"To: {groupName}\nSubject: {subject}\n\n{body}{attachments}";
            File.WriteAllText(Path.Combine(directoryPath, fileName), fileContents);
            _log.Info($"{subject} E-mail for {groupName} saved as: {fileName}");            
        }

        public void Send(SenderMailboxType mailbox, string to, string templateTypeName, Dictionary<string, string> values)
        {
            var content = _mailTemplateRenderer.Render(templateTypeName, values);
            Send(mailbox, to, content.Subject, content.Body);
        }
    }
}
