using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public MaterialBatchViewModel() { }

        public int Id { get; set; }

        public string MaterialName { get; set; }

        public decimal Volume { get; set; }

        public string UnitName { get; set; }

        public string AuthorName { get; set; }

        public string BatchNumber { get; set; }

        public string DisplayDt { get; set; }

        public long SortDt { get; }

        public decimal? Price { get; set; }

        public string InvoiceNumber { get; set; }
    }
}
