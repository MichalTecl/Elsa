using Elsa.Common.Utils;
using Elsa.Smtp.Core.Database;
using Robowire;

namespace Elsa.Smtp.Core
{
    public class SmtpRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            if (AppEnvironment.IsDev)
            {
                setup.For<IMailSender>().Use<DebugMailSender>();
            }
            else
            {
                setup.For<IMailSender>().Use<SmtpMailSender>();
            }

            setup.For<IRecipientListsRepository>().Use<RecipientListsRepository>();
        }
    }
}
