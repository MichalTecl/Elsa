using Elsa.App.ImportExport;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Elsa.Apps.Inventory.XlsBulkStockEvents
{
    public class BulkStockEventXlsModule : XlsImportExportModuleBase<StockEventXlsModel>
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IStockEventRepository _eventRepository;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly AmountProcessor _amountProcessor;
        private readonly IDatabase _db;

        public BulkStockEventXlsModule(IStockEventRepository eventRepository, IMaterialBatchFacade batchFacade, IMaterialBatchRepository batchRepository, AmountProcessor amountProcessor, IMaterialRepository materialRepository, IDatabase db)
        {
            _eventRepository = eventRepository;
            _batchFacade = batchFacade;
            _batchRepository = batchRepository;
            _amountProcessor = amountProcessor;
            _materialRepository = materialRepository;
            _db = db;
        }

        public override string Title => "Hromadný přesun zbytků šarží";

        public override string Description => "Pomocí uploadu XLS hromadně přesune zbývající množství vložených šarží do odpadu/propagace. Použijte tlačítko \"Stáhnout\" pro získání šablony XLS";
                
        protected override List<StockEventXlsModel> ExportData(out string exportFileName)
        {
            exportFileName = "HromadnýPřesun_ŠABLONA.xlsx";

            return _eventRepository.GetAllEventTypes().Select(evt =>
                new StockEventXlsModel
                {
                    MaterialName = "Název materiálu",
                    BatchNumber = "Číslo šarže",
                    EventName = evt.Name,
                    Note = evt.RequiresNote ? $"Přesun do \"{evt.Name}\" vyžaduje poznámku, vysvětlující důvod přesunu" : null
                })
                .ToList();
        }

        protected override string ImportData(List<StockEventXlsModel> data)
        {
            using (var tx = _db.OpenTransaction()) 
            {
                foreach (var d in data)
                {
                    var material = _materialRepository.GetMaterialByName(d.MaterialName);
                    if (material == null)
                        throw new ArgumentException($"Materiál \"{d.MaterialName}\" neexistuje");

                    var batchKey = new Commerce.Core.Model.BatchKey(material.Id, d.BatchNumber);

                    var batches = _batchRepository.GetBatches(batchKey).ToList();
                    if (batches.Count == 0)
                        throw new ArgumentException($"Nebyla nalezena šarže \"{d.BatchNumber}\" materiálu \"{d.MaterialName}\"");

                    var eventType = _eventRepository.GetAllEventTypes().FirstOrDefault(et => et.Name.Equals(d.EventName?.Trim(), StringComparison.InvariantCultureIgnoreCase))
                        ?? throw new ArgumentException($"\"{d.EventName}\" není platný cíl přesunu");

                    if (eventType.RequiresNote && string.IsNullOrWhiteSpace(d.Note))
                        throw new ArgumentException($"Pro přesun do \"{d.EventName}\" je vyžadována poznámka");

                    var available = _batchFacade.GetAvailableAmount(batchKey);

                    if (available.IsZero)
                        throw new ArgumentException($"Šarže \"{d.BatchNumber}\" materiálu \"{d.MaterialName}\" nemá žádné zbývající množství");
                    
                    _eventRepository.SaveEvent(eventType.Id, material.Id, d.BatchNumber, available.Value, d.Note, available.Unit.Symbol);
                }

                tx.Commit();
            }

            return $"Hotovo. Zpracováno {data.Count} šarží";
        }
    }
}
