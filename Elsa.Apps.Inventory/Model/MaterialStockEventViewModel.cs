using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialStockEventViewModel
    {
        public MaterialStockEventViewModel(IMaterialStockEvent e)
        {
            Id = e.Id;
            Dt = StringUtil.FormatDateTime(e.EventDt);
            Author = e.User.EMail;
            Content = $"{StringUtil.FormatDecimal(e.Volume)}{e.Unit.Symbol} {e.Material.Name}";
            Note = e.Description;
            Time = e.EventDt.Ticks;
        }

        public int Id { get; }

        public string Dt { get; }

        public string Author { get; }

        public string Content { get; }

        public string Note { get; }

        public long Time { get; }
    }
}
