﻿
using Elsa.Common.Utils;
using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model
{
    public class PriceComponentModel
    {
        public PriceComponentModel(int batchId)
        {
            BatchId = batchId;
        }

        public int BatchId { get; }

        public string Text { get; set; }

        public string Value => StringUtil.FormatPrice(RawValue);

        public bool IsWarning { get; set; }

        [JsonIgnore]
        internal decimal RawValue { get; set; }

        [JsonIgnore]
        internal int? SourceBatchId { get; set; }
    }
}
