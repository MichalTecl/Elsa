using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderImportFailureModel
    {
        public int Id { get; set; }

        public string ErpName { get; set; }

        public string OrderNumber { get; set; }

        public DateTime FirstFailureDt { get; set; }

        public DateTime LastFailureDt { get; set; }

        public int FailureCount { get; set; }

        public string LastError { get; set; }
    }
}
