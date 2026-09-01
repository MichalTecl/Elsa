using System.Collections.Generic;

using Elsa.App.Emailing.Model;

namespace Elsa.App.Emailing.Internal
{
    public interface IMailTemplateRepository
    {
        List<MailTemplateModel> GetAll();

        MailTemplateModel Get(int? id);

        MailTemplateModel GetByTypeName(string typeName);

        MailTemplateModel Save(MailTemplateModel model);

        void Delete(int id);
    }
}
