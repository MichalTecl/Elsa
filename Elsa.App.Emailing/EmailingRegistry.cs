using Elsa.App.Emailing.Internal;
using Elsa.Smtp.Core;

using Robowire;

namespace Elsa.App.Emailing
{
    public class EmailingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IMailTemplateRepository>().Use<MailTemplateRepository>();
            setup.For<IMailTemplateRenderer>().Use<MailTemplateRenderer>();
        }
    }
}
