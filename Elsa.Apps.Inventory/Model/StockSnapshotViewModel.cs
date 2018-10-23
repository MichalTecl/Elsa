using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Apps.Inventory.Model
{
    public class StockSnapshotViewModel
    {
        public StockSnapshotViewModel(IStockLevelSnapshot s)
        {
            MaterialId = s.Material?.Id ?? s.MaterialId;
            MaterialName = s.Material?.Name;


            if (s.Id > 0)
            {
                Value = $"{StringUtil.FormatDecimal(s.Volume)}{s.Unit.Symbol}";
                Author = s.User?.EMail;
                Id = s.Id;
                Dt = StringUtil.FormatDateTime(s.SnapshotDt);
                DtText = DateUtil.FormatDateWithAgo(s.SnapshotDt);
                Note = s.Note;
            }
            else
            {
                Dt = DtText = "Nikdy";
            }
        }

        public long Id { get; }

        public int MaterialId { get; }

        public string MaterialName { get; }

        public string Author { get; }

        public string Value { get; }

        public string Dt { get; }

        public string DtText { get; }

        public string Note { get; }
    }
}
