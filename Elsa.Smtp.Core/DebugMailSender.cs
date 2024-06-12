using Elsa.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Elsa.Smtp.Core
{
    public class DebugMailSender : IMailSender
    {
        private readonly ILog _log;

        public DebugMailSender(ILog log)
        {
            _log = log;
        }

        public void Send(string to, string subject, string body, params string[] attachmentFiles)
        {
            SendToGroup(to, subject, body, attachmentFiles);
        }

        public void SendToGroup(string groupName, string subject, string body, params string[] attachmentFiles)
        {            
            string directoryPath = @"C:\Elsa\Log\MailSender";
            Directory.CreateDirectory(directoryPath);

            var uniqueIdentifier = Guid.NewGuid().ToString();
            var fileName = $"{subject}_{uniqueIdentifier}.txt";
            
            var attachments = attachmentFiles.Length > 0 ? "\n\nATTACHMENTS: " + string.Join(", ", attachmentFiles) : string.Empty;

            var fileContents = $"To: {groupName}\nSubject: {subject}\n\n{body}{attachments}";
            File.WriteAllText(Path.Combine(directoryPath, fileName), fileContents);
            _log.Info($"{subject} E-mail for {groupName} saved as: {fileName}");            
        }
    }
}
