using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;

using Elsa.App.Emailing.Internal;
using Elsa.App.Emailing.Model;
using Elsa.Apps.CommonData;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Smtp.Core;

using Robowire.RoboApi;

namespace Elsa.App.Emailing
{
    [Controller("mailTemplates")]
    public class MailTemplatesController : ElsaControllerBase
    {
        private readonly IMailTemplateRepository _repository;
        private readonly IMailSender _mailSender;
        private readonly IMailTemplateRenderer _mailTemplateRenderer;

        public MailTemplatesController(
            IWebSession webSession,
            ILog log,
            IMailTemplateRepository repository,
            IMailSender mailSender,
            IMailTemplateRenderer mailTemplateRenderer)
            : base(webSession, log)
        {
            _repository = repository;
            _mailSender = mailSender;
            _mailTemplateRenderer = mailTemplateRenderer;
        }

        public List<MailTemplateModel> GetAll()
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            return _repository.GetAll();
        }

        public MailTemplateModel Get(int? id)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            var template = _repository.Get(id);
            template.BodyBase64 = System.Convert.ToBase64String(
                Encoding.UTF8.GetBytes(template.Body ?? string.Empty));
            template.Body = null;
            return template;
        }

        public MailTemplateModel Save(MailTemplateModel model)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            var savedTemplate = _repository.Save(model);
            savedTemplate.Body = null;
            return savedTemplate;
        }

        public List<MailTemplateModel> Delete(int id)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            _repository.Delete(id);
            return _repository.GetAll();
        }

        public MailTemplateTestSettings GetTestMailSettings()
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            return new MailTemplateTestSettings
            {
                Recipient = WebSession.User.EMail
            };
        }

        public void SendTestMail(MailTemplateTestRequest request)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);

            if (request == null)
            {
                throw new System.ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                throw new System.InvalidOperationException("E-mail příjemce musí být vyplněný.");
            }

            string recipient;
            try
            {
                recipient = new MailAddress(request.Recipient.Trim()).Address;
            }
            catch (System.FormatException)
            {
                throw new System.InvalidOperationException("E-mail příjemce nemá platný formát.");
            }

            SenderMailboxType mailbox;
            if (request.MailboxType == SenderMailboxType.SystemRobot.TypeName)
            {
                mailbox = SenderMailboxType.SystemRobot;
            }
            else if (request.MailboxType == SenderMailboxType.CustomerFacingSender.TypeName)
            {
                mailbox = SenderMailboxType.CustomerFacingSender;
            }
            else
            {
                throw new System.InvalidOperationException("Vybraný typ odesílatele není podporovaný.");
            }

            var content = _mailTemplateRenderer.RenderContent(
                request.Subject,
                request.Body,
                request.BodyFormat,
                request.Values);
            _mailSender.Send(mailbox, recipient, content);
        }

        public FileResult GetPicture(string path)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);

            var picturePath = MailTemplatePictureStore.ResolvePath(path);
            return new FileResult(
                Path.GetFileName(picturePath),
                File.ReadAllBytes(picturePath),
                MailTemplatePictureStore.GetContentType(picturePath),
                "inline")
            {
                DisableBrowserCache = true
            };
        }
    }
}
