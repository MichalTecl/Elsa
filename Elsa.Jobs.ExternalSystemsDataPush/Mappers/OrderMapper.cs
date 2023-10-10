using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Integration.Crm.Raynet.Model;
using Elsa.Jobs.ExternalSystemsDataPush.Model;

namespace Elsa.Jobs.ExternalSystemsDataPush.Mappers
{
    public static class OrderMapper
    {
        public static BusinessCaseModel ToBcModel(OrderExportModel order, List<ProductListItem> productListItems, Elsa.Common.Logging.ILog log) 
        {
            var bc = new BusinessCaseModel
            {
                Name = order.OrderNr,
                Company = IdContainer.Get(long.Parse(order.CustomerRayNetId)),
                TotalAmount = order.OrderPrice,
                ValidFrom = order.BuyDate.ToString("yyyy-MM-dd"),
                Status = "E_WIN",
                BusinessCasePhase = IdContainer.Get(5)
            };

            foreach(var item in order.Items) 
            {
                var prodCode = productListItems.FirstOrDefault(product => product.Code.Equals(item.ProductUid, StringComparison.InvariantCultureIgnoreCase))?.Code;
                if (prodCode == null) 
                {
                    log.Info($"Cannot process order {order.OrderNr} because it's item code={item.ProductUid} (\"{item.ProductName}\") does not exist in RN product list");
                    return null;
                }

                bc.Items.Add(new BcItemModel
                {
                    ProductCode = prodCode,
                    // Name = item.ProductName,
                    Price = (item.ItemTaxedPrice / item.ItemQuantity) / (1 + (item.ProductTaxPercent / 100m)),
                    // TaxRate = item.ProductTaxPercent,
                    Count = item.ItemQuantity,
                    DiscountPercent = order.DiscountPercent                    
                });
            }

            return bc;
        }
    }
}
