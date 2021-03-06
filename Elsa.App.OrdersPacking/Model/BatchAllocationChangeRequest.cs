﻿using System;

namespace Elsa.App.OrdersPacking.Model
{
    public class BatchAllocationChangeRequest
    {
        public long OrderId { get; set; }

        public long OrderItemId { get; set; }

        public string OriginalBatchNumber { get; set; }

        public decimal? NewAmount { get; set; }

        public string NewBatchSearchQuery { get; set; }
    }
}
