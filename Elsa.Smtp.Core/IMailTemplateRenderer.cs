using System.Collections.Generic;

namespace Elsa.Smtp.Core
{
    public interface IMailTemplateRenderer
    {
        MailTemplateContent Render(string templateTypeName, Dictionary<string, string> values);
    }
}
