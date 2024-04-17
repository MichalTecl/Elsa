using Elsa.App.ImportExport;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using XlsSerializer.Core.Attributes;

namespace Elsa.Commerce.Core.ImportExportModules
{
    public class AbandonedBatchRulesImpExp : XlsImportExportModuleBase<AbandonedBatchRuleModel>
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly ISession _session;

        public AbandonedBatchRulesImpExp(IMaterialRepository materialRepository, IDatabase db, ISession session) : base(db)
        {
            _materialRepository = materialRepository;
            _session = session;
        }

        public override string Title => "Pravidla pro opuštěné šarže";

        public override string Description => "Import/Export nastavení pravidel pro řešení opuštěných šarží pro jednotlivé materiály";

        protected override List<AbandonedBatchRuleModel> ExportData(out string exportFileName, IDatabase db)
        {
            exportFileName = "PravidlaOpustenychSarzi.xlsx";

            var allMaterials = _materialRepository.GetAllMaterials(null).OrderBy(m => m.InventoryId).OrderBy(m => m.Name);

            var res = allMaterials.Select(m => new AbandonedBatchRuleModel
            {
                Material = m.Name,
                ReportDays = m.DaysBeforeWarnForUnused,
                EventProlongs = m.UsageProlongsLifetime,
                Autofinalize = m.Autofinalization,
                ReportGroup = m.UnusedWarnMaterialType,
                NotAbandonedUntilNewerBatchUsed = m.NotAbandonedUntilNewerBatchUsed
            }).ToList();

            return res;
        }

        protected override string ImportDataInTransaction(List<AbandonedBatchRuleModel> data, IDatabase db, ITransaction tx)
        {
            var allMats = db.SelectFrom<IMaterial>().Where(m => m.ProjectId == _session.Project.Id).Execute().ToDictionary(k => k.Name, k => k);

            int changed = 0;

            foreach (var row in data)
            {
                if (!allMats.TryGetValue(row.Material, out var material))
                    throw new ArgumentException($"Neznámý materiál \"{row.Material}\"");

                bool upd = false;

                if (material.DaysBeforeWarnForUnused != row.ReportDays)
                {
                    material.DaysBeforeWarnForUnused = row.ReportDays;
                    upd = true;
                }

                if ((material.UsageProlongsLifetime ?? false) != row.EventProlongs)
                {
                    material.UsageProlongsLifetime = row.EventProlongs;
                    upd = true;
                }

                if ((material.UseAutofinalization ?? false) != row.Autofinalize)
                {
                    material.UseAutofinalization = row.Autofinalize;
                    upd = true;
                }

                if ((material.NotAbandonedUntilNewerBatchUsed ?? false) != row.NotAbandonedUntilNewerBatchUsed)
                {
                    material.NotAbandonedUntilNewerBatchUsed = row.NotAbandonedUntilNewerBatchUsed;
                    upd = true;
                }

                var rg = string.IsNullOrWhiteSpace(row.ReportGroup) ? null : row.ReportGroup.Trim();
                if (material.UnusedWarnMaterialType != rg)
                {
                    material.UnusedWarnMaterialType = rg;
                    upd = true;
                }

                if (row.ReportDays != null)
                {
                    if ((!row.Autofinalize) && (rg == null))
                    {
                        throw new ArgumentException($"Neplatné nastavení pro materiál \"{row.Material}\". Pokud se nemá automaticky přesouvat do odpadu, musí být vyplněna skupina pro reportování");
                    }

                    if (row.Autofinalize && rg != null)
                    {
                        throw new ArgumentException($"Neplatné nastavení pro materiál \"{row.Material}\". Pokud se má automaticky přesouvat do odpadu, nesmí být vyplněna skupina pro reportování");
                    }
                }

                if (!upd)
                    continue;

                db.Save(material);

                changed++;
            }

            if (changed > 0)
                _materialRepository.CleanCache();

            return $"Hotovo. Byla změněna pravidla pro {changed} materiálů.";
        }
    }

    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class AbandonedBatchRuleModel
    {
        [XlsColumn("A", "Materiál")]
        public string Material { get; set; }

        [XlsColumn("B", "Počet dnů")]
        public int? ReportDays { get; set; }

        [XlsColumn("C", "Od posledního použití")]
        public bool EventProlongs { get; set; }

        [XlsColumn("D", "Pokud byla použita novější šarže")]
        public bool NotAbandonedUntilNewerBatchUsed { get; set; }

        [XlsColumn("E", "Automaticky Odpad")]
        public bool Autofinalize { get; set; }

        [XlsColumn("F", "Reportovat ve skupině")]
        public string ReportGroup { get; set; }
    }
}
