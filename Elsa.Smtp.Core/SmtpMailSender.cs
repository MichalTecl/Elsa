using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Logging;

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
                var fromAddress = new MailAddress(m_settings.SenderAddress, m_settings.SenderName);
                var toAddress = new MailAddress(to);

                var smtp = new SmtpClient
                {
                    Host = m_settings.SmtpHost,
                    Port = m_settings.SmtpPort,
                    EnableSsl = m_settings.EnableSsl,
                    DeliveryMethod = m_settings.DeliveryMethod,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, m_settings.SenderPassword)
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
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
