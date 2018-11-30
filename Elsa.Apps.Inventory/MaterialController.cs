using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("material")]
    public class MaterialController : ElsaControllerBase
    {
        private readonly IMaterialFacade m_materialFacade;

        public MaterialController(IWebSession webSession, ILog log, IMaterialFacade materialFacade)
            : base(webSession, log)
        {
            m_materialFacade = materialFacade;
        }

        public MaterialSetupInfo GetMaterialInfo(string materialName)
        {
            var info = m_materialFacade.GetMaterialInfo(materialName);
            if (info?.MaterialId == null)
            {
                return null;
            }

            return info;
        }

        public IEnumerable<MaterialSetupInfo> GetAllMaterialInfo()
        {
            return m_materialFacade.GetAllMaterialInfo();
        }
    }
}
