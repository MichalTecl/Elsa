﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportEntry : BatchReportEntryBase
    {
        public string CustomField1 { get; set; }
        public string CustomField2 { get; set; }
        public string CustomField3 { get; set; }
        public string InventoryName { get; set; }
        public string BatchNumber { get; set; }
        public string MaterialName { get; set; }
        public int MaterialId { get; set; }
        public string BatchVolume { get; set; }
        public string AvailableAmount { get; set; }

        [JsonIgnore]
        public Amount Available { get; set; }
        public string CreateDt { get; set; }
        public bool IsClosed { get; set; }
        public bool IsLocked { get; set; }
        public bool IsAvailable { get; set; }
        public int NumberOfComponents { get; set; }
        public int NumberOfCompositions { get; set; }
        public int NumberOfOrders { get; set; }
        public string Price { get; set; }
        public string InvoiceNumber { get; set; }
        public bool HasStockEvents { get; set; }
        public Dictionary<string, int> StockEventCounts { get; set; } = new Dictionary<string, int>();

        public int NumberOfSaleEvents { get; set; }
        public int NumberOfSegments { get; set; }
        public decimal AvailableAmountValue { get; set; }
        public int AvailableAmountUnitId { get; set; }       
        public decimal TotalAmountValue { get; set; }
        public string TotalAmountUnitName { get; set; }        

        public BatchReportEntry(BatchKey batchKey) : base(batchKey)
        {
        }
    }
}
