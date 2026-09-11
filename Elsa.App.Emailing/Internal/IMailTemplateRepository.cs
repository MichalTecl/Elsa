using System.Collections.Generic;

using Elsa.App.Emailing.Model;

namespace Elsa.App.Emailing.Internal
{
    public interface IMailTemplateRepository
    {
        List<MailTemplateModel> GetAll();

        MailTemplateModel Get(string typeName);

        MailTemplateModel GetByTypeName(string typeName);

        bool Exists(string typeName);

        MailTemplateModel Save(MailTemplateModel model);

        void Delete(string typeName, string bodyFormat);
    }
}
