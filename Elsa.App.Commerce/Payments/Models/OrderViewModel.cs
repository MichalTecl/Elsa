using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.App.Commerce.Payments.Models
{
    public class OrderViewModel
    {
        public OrderViewModel(IPurchaseOrder source)
        {
            OrderId = source.Id;
            VariableSymbol = source.VarSymbol;
            OrderDate = DateUtil.FormatDateWithAgo(source.PurchaseDate);
            Source = source.Erp.Description;
            CustomerName = $"{source.InvoiceAddress.FirstName} {source.InvoiceAddress.LastName}";
            Price = $"{((double)source.PriceWithVat)} {source.Currency.Symbol}";
            CustomerCompany = StringUtil.Nvl(source.InvoiceAddress.CompanyName, source.DeliveryAddress?.CompanyName);
            Message = source.CustomerNote;
        }

        public long OrderId { get; set; }
        public string VariableSymbol { get; set; }
        public string OrderDate { get; set; }
        public string Source { get; set; }
        public string CustomerName { get; set; }
        public string Price { get; set; }
        public string CustomerCompany { get; set; }

        public string Message { get; set; }
    }
}
