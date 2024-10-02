using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public class KitNoteParseResultModel
    {
        public int Id { get; set; }
        public long OrderId { get; set; }
        public int KitNr { get; set; }
        public string KitName { get; set; }
        public string ItemType { get; set; }
        public string Item { get; set; }
        public int? KitDefinitionId { get; set; }
        public int? SelectionGroupId { get; set; }
        public int? SelectionGroupItemId { get; set; }
    }
}
