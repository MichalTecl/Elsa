using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorOrderInfo
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; }   
        public decimal PriceWithoutTax { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; }
        public string Discounts { get; set; }

        public string PriceWithoutTaxF => StringUtil.FormatPrice(PriceWithoutTax);
        public string PurchaseDateF => StringUtil.FormatDate(PurchaseDate);
    }

    public class DistributorOrderInfoPage
    {
        public DistributorOrderInfoPage(IEnumerable<DistributorOrderInfo> rows, int pageSize)
        {
            Data = new List<DistributorOrderInfo>(rows);

            if (Data.Count < pageSize)
                return;

            NextPageKey = Data.Min(d => d.Id);
        }

        public long? NextPageKey { get; set; }

        public List<DistributorOrderInfo> Data { get; set; }
    }
}
