using System.Collections.Generic;

namespace Elsa.Smtp.Core
{
    public interface IMailTemplateRenderer
    {
        MailTemplateContent Render(string templateTypeName, Dictionary<string, string> values);

        MailTemplateContent RenderContent(
            string subject,
            string body,
            string bodyFormat,
            Dictionary<string, string> values);
    }
}
