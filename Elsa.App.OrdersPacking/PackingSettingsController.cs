using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using XlsSerializer.Core;

namespace Elsa.App.OrdersPacking
{
    [Controller("PackingSettings")]
    public class PackingSettingsController : ElsaControllerBase
    {
        private readonly IVirtualProductRepository m_vpRepo;

        public PackingSettingsController(IWebSession webSession, ILog log, IVirtualProductRepository vpRepo) : base(webSession, log)
        {
            m_vpRepo = vpRepo;
        }

        public FileResult GetProductMappings()
        {
            var export = m_vpRepo.ExportErpProductMappings();

            var bytes = XlsxSerializer.Instance.Serialize(export);

            return new FileResult("Eshop_Elsa_Mapping.xlsx", bytes);
        }

        public void UploadProductMappings(RequestContext context)
        {
            var file = context.HttpContext.Request.Files[0];

            var map = XlsxSerializer.Instance.Deserialize<List<ErpProductMapping>>(file.InputStream);

            m_vpRepo.ImportErpProductMappings(map);
        }

        public FileResult GetKitDefinitions()
        {
            var export = m_vpRepo.ExportKits();

            var bytes = XlsxSerializer.Instance.Serialize(export);

            return new FileResult("Definice_Sad.xlsx", bytes);
        }

        public void UploadKitDefinitions(RequestContext context)
        {
            var file = context.HttpContext.Request.Files[0];

            var map = XlsxSerializer.Instance.Deserialize<List<KitProductXlsModel>>(file.InputStream);

            m_vpRepo.ImportKits(map);
        }
    }
}
