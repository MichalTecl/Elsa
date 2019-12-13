using System.Collections.Generic;
using Elsa.Common;
using Elsa.Common.Configuration;
using Robowire.RoboApi;

namespace Elsa.App.Profile
{
    [Controller("UserProfile")]
    public class ProfileController
    {
        private readonly IConfigurationRepository m_configurationRepository;

        public ProfileController(IConfigurationRepository configurationRepository)
        {
            m_configurationRepository = configurationRepository;
        }

        public string Hello()
        {
            return "Hello";
        }

        [AllowAnonymous]
        public Dictionary<string, string> GetClientConfig()
        {
            return m_configurationRepository.GetClientVisibleConfig();
        }
    }
}
