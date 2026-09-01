using System.Collections.Generic;

using Elsa.App.Emailing.Internal;
using Elsa.App.Emailing.Model;
using Elsa.Apps.CommonData;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.Emailing
{
    [Controller("mailTemplates")]
    public class MailTemplatesController : ElsaControllerBase
    {
        private readonly IMailTemplateRepository _repository;

        public MailTemplatesController(IWebSession webSession, ILog log, IMailTemplateRepository repository)
            : base(webSession, log)
        {
            _repository = repository;
        }

        public List<MailTemplateModel> GetAll()
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            return _repository.GetAll();
        }

        public MailTemplateModel Get(int? id)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            return _repository.Get(id);
        }

        public MailTemplateModel Save(MailTemplateModel model)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            return _repository.Save(model);
        }

        public List<MailTemplateModel> Delete(int id)
        {
            EnsureUserRight(CommonDataUserRights.SettingsApp);
            _repository.Delete(id);
            return _repository.GetAll();
        }
    }
}
