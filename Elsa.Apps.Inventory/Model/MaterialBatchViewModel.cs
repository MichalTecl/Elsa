using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialBatchViewModel
    {
        public MaterialBatchViewModel(IMaterialBatch batch)
        {
            Id = batch.Id;
            MaterialName = batch.Material.Name;
            Volume = batch.Volume;
            UnitName = batch.Unit.Symbol;
            AuthorName = batch.Author.EMail;
            BatchNumber = batch.BatchNumber;
            DisplayDt = StringUtil.FormatDateTime(batch.Created);
            SortDt = batch.Created.Ticks;
            Price = batch.Price;
            InvoiceNumber = batch.InvoiceNr;
            MaterialId = batch.MaterialId;
            AutomaticBatches = batch.Material.AutomaticBatches;
        }

        public MaterialBatchViewModel() { }

        public bool AutomaticBatches { get; set; }

        public int? Id { get; set; }

        public string MaterialName { get; set; }

        public int MaterialId { get; set; }

        public decimal Volume { get; set; }

        public string UnitName { get; set; }

        public string AuthorName { get; set; }

        public string BatchNumber { get; set; }

        public string DisplayDt { get; set; }

        public long SortDt { get; }

        public decimal? Price { get; set; }

        public string InvoiceNumber { get; set; }

        public static IEnumerable<MaterialBatchViewModel> JoinAutomaticBatches(IEnumerable<MaterialBatchViewModel> source, AmountProcessor processor)
        {
            var targetList = new List<MaterialBatchViewModel>();
            
            foreach (var batch in source.OrderBy(i => i.MaterialId))
            {
                if (batch.AutomaticBatches && targetList.LastOrDefault()?.MaterialId == batch.MaterialId)
                {
                    var joined = targetList.Last();
                    joined.BatchNumber = null;
                    joined.Id = null;

                    var sum = processor.Add(processor.ToAmount(batch.Volume, batch.UnitName),
                        processor.ToAmount(joined.Volume, joined.UnitName));

                    joined.UnitName = sum.Unit.Symbol;
                    joined.Volume = sum.Value;

                    continue;
                }

                targetList.Add(batch);
            }

            return targetList;
        }
    }
}
