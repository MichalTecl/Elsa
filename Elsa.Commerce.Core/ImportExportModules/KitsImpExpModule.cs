using Elsa.App.ImportExport;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.ImportExportModules
{
    public class KitsImpExpModule : XlsImportExportModuleBase<KitProductXlsModel>
    {
        private readonly IVirtualProductRepository _vpRepo;

        public KitsImpExpModule(IVirtualProductRepository vpRepo, IDatabase db) : base(db)
        {
            _vpRepo = vpRepo;
        }

        public override string Title => "Definice sad";

        public override string Description => "Nastavení sad produktů";

        protected override List<KitProductXlsModel> ExportData(out string exportFileName, IDatabase db)
        {
            exportFileName = "Definice_Sad.xlsx";
            return _vpRepo.ExportKits();
        }

        protected override string ImportDataInTransaction(List<KitProductXlsModel> data, IDatabase db, ITransaction tx)
        {
            var cnt = _vpRepo.ImportKits(data);

            return $"Hotovo. {cnt} sad bylo importováno.";
        }
    }
}
