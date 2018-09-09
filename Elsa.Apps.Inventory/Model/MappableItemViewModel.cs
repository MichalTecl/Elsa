
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;

using Newtonsoft.Json;

namespace Elsa.Apps.Inventory.Model
{
    public class MappableItemViewModel
    {
        private string m_searchText;

        private string m_id;
        
        public string Id
        {
            get
            {
                if (m_id == null)
                {
                    var id = new ItemId
                                 {
                                     ErpId = ErpId,
                                     ErpItemId = ErpProductId,
                                     OrderItemText = OrderItemText
                                 };
                    m_id = JsonConvert.SerializeObject(id);
                }

                return m_id;
            }
        }

        public int? ErpId { get; set; }

        public string ErpName { get; set; }

        public string ErpProductId { get; set; }

        public string OrderItemText { get; set; }

        public List<VirtualProductViewModel> AssignedVirtualProducts { get; set; } = new List<VirtualProductViewModel>();

        [JsonIgnore]
        public string SearchText => m_searchText ?? (m_searchText = StringUtil.NormalizeSearchText(9999, GetSearchTokens()));

        private IEnumerable<string> GetSearchTokens()
        {
            yield return ViewText;

            foreach (var vp in AssignedVirtualProducts)
            {
                yield return vp.Name;
            }
        }

        public string ViewText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ErpName))
                {
                    return OrderItemText;
                }
                else
                {
                    return $"{ErpName.ToUpperInvariant()}: {OrderItemText}";
                }
            }
        }

        public static ItemId ParseId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<ItemId>(id);
        }

        public sealed class ItemId
        {
            public int? ErpId { get; set; }
            public string ErpItemId { get; set; }
            public string OrderItemText { get; set; }
        }

        public static MappableItemViewModel Create(IVirtualProductMappableItem source, IErp erp)
        {
            return new MappableItemViewModel()
                       {
                           ErpId = erp?.Id,
                           ErpName = erp?.Description,
                           ErpProductId = source.ErpProductId,
                           OrderItemText = source.ItemName
                       };
        }
    }
}
