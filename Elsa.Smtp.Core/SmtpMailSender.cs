using System;

using Elsa.Common.Logging;
using MailKit.Net.Smtp;
using MimeKit;

namespace Elsa.Smtp.Core
{
    public class SmtpMailSender : IMailSender
    {
        private readonly SmtpSettings m_settings;
        private readonly ILog m_log;

        public SmtpMailSender(SmtpSettings settings, ILog log)
        {
            m_settings = settings;
            m_log = log;
        }

        public void Send(string to, string subject, string body)
        {
            m_log.Info($"Sending e-mail to: {to}, subject: {subject}");

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(m_settings.SenderName, m_settings.SenderAddress));
                mailMessage.To.Add(new MailboxAddress(to, to));
                mailMessage.Subject = subject;
                mailMessage.Body = new TextPart("plain")
                {
                    Text = body
                };

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect(m_settings.SmtpHost, m_settings.SmtpPort, true);
                    smtpClient.Authenticate(m_settings.SenderAddress, m_settings.SenderPassword);
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }
                
                m_log.Info("Sent");
            }
            catch (Exception ex)
            {
                m_log.Error($"Sending e-mail to: {to}, subject: {subject} failed", ex);
                throw;
            }
        }
    }
}
