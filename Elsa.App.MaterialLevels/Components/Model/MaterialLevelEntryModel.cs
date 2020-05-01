﻿using System.Collections.Generic;
using Elsa.Common;
using Newtonsoft.Json;

namespace Elsa.App.MaterialLevels.Components.Model
{
    public class MaterialLevelEntryModel
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        [JsonIgnore]
        public string DefaultUnitSymbol { get; set; }

        public List<BatchAmountModel> Batches { get; } = new List<BatchAmountModel>();

        [JsonIgnore]
        public Amount Total { get; set; }

        public string TotalFormatted => Total?.ToString();

        [JsonIgnore]
        public Amount Threshold { get; set; }

        public string ThresholdFormatted => Threshold?.ToString();

        public bool HasWarning { get; set; }

        public string UnitSymbol => Threshold?.Unit.Symbol ?? Total?.Unit.Symbol ?? DefaultUnitSymbol;

        public string SupplierName { get; set; }

        public string SupplierEmail { get; set; }

        public string SupplierPhone { get; set; }
    }

    public class BatchAmountModel
    {
        public string BatchNumber { get; set; }

        [JsonIgnore]
        public Amount Amount { get; set; }

        [JsonIgnore]
        public int UnitId { get; set; }

        [JsonIgnore]
        public decimal Value { get; set; }

        public string AmountFormatted => Amount?.ToString();
    }
}
