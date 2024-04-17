using Elsa.App.ImportExport;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.ImportExportModules
{
    public class EshopProductMappingsImpExpModule : XlsImportExportModuleBase<ErpProductMapping>
    {
        private readonly IVirtualProductRepository _vpRepo;

        public EshopProductMappingsImpExpModule(IVirtualProductRepository vpRepo, IDatabase database) : base(database)
        {
            _vpRepo = vpRepo;
        }

        public override string Title => "Mapování produktů e-shopu";

        public override string Description => "Mapping materiálů v Else na názvy produktů v E-Shopu";

        protected override List<ErpProductMapping> ExportData(out string exportFileName, IDatabase db)
        {
            exportFileName = "Eshop_Elsa_Mapping.xlsx";
            return _vpRepo.ExportErpProductMappings();
        }

        protected override string ImportDataInTransaction(List<ErpProductMapping> data, IDatabase db, ITransaction tx)
        {
            var count = _vpRepo.ImportErpProductMappings(data);

            return $"Hotovo. Bylo uloženo {count} změn.";
        }
    }
}
