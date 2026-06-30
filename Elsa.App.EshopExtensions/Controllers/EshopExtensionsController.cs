using Elsa.App.EshopExtensions.Internal;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;

namespace Elsa.App.EshopExtensions.Controllers
{
    [Controller("eshopExtensions")]
    public class EshopExtensionsController : ElsaControllerBase
    {
        private readonly IEshopExtensionsRepository _repository;

        public EshopExtensionsController(IWebSession webSession, ILog log, IEshopExtensionsRepository repository)
            : base(webSession, log)
        {
            _repository = repository;
        }

        public EshopExtensionsStatus GetStatus()
        {
            return _repository.GetStatus();
        }
    }
}
