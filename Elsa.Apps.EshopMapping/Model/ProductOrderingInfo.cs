using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Elsa.Apps.EshopMapping.Model
{
    public class ProductOrderingInfo
    {
        public string PlacedName { get; set; }
        public int? OrderCountNoKit { get; set; }

        [JsonIgnore]
        public DateTime? LastOrderNoKit { get; set; }
        public int? OrderCountInKit { get; set; }

        [JsonIgnore]
        public DateTime? LastOrderInKit { get; set; }

        public string LastOrderDtNoKit => StringUtil.FormatDate(LastOrderNoKit);
        public string LastOrderDtKit => StringUtil.FormatDate(LastOrderInKit);
    }
}
