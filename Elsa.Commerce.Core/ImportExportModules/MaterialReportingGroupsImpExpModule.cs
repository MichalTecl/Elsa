using Elsa.App.ImportExport;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.ImportExportModules
{
    public class MaterialReportingGroupsImpExpModule : XlsImportExportModuleBase<MaterialReportingGroupAssignmentModel>
    {
        private readonly IMaterialRepository _repo;

        public MaterialReportingGroupsImpExpModule(IMaterialRepository repo)
        {
            _repo = repo;
        }

        public override string Title => "Skupiny produktů";

        public override string Description => "Přiřazení produktů (materiálů v Else) do skupin pro reporting. Prázdná hodnota ve sloupci skupiny odebere materiál od skupiny, neexistující skupina bude vytvořena.";

        protected override List<MaterialReportingGroupAssignmentModel> ExportData(out string exportFileName)
        {
            exportFileName = "SkupinyProduktu.xlsx";
            return _repo.GetMaterialReportingGroupAssignments();
        }

        protected override string ImportData(List<MaterialReportingGroupAssignmentModel> data)
        {
            _repo.SaveMaterialReportingGroupAssignments(data, out var groupsCreatedCount, out var assignmentsCount);

            var message = $"Hotovo. Bylo změněno zařazení do skupin u {assignmentsCount} materiálů.";

            if (groupsCreatedCount > 0)
                message += $" {groupsCreatedCount} nových skupin bylo vytvořeno.";

            return message;
        }
    }
}
