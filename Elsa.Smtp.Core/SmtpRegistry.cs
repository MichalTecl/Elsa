using Robowire;

namespace Elsa.Smtp.Core
{
    public class SmtpRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IMailSender>().Use<SmtpMailSender>();
        }
    }
}
