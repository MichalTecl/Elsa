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
        public static BusinessCaseModel ToBcModel(OrderExportModel order) 
        {
            var bc = new BusinessCaseModel
            {
                Name = order.OrderNr,
                Company = long.Parse(order.CustomerRayNetId),
                TotalAmount = order.OrderPrice,
                ValidFrom = order.BuyDate.ToString("yyyy-MM-dd"),
                Status = "E_WIN",
                BusinessCasePhase = 5
            };

            foreach(var item in order.Items) 
            {
                bc.Items.Add(new BcItemModel
                {
                    ProductCode = item.ProductUid,
                    Name = item.ProductName,
                    Price = (item.ItemTaxedPrice / item.ItemQuantity) / (1 + (item.ProductTaxPercent / 100m)),
                    TaxRate = item.ProductTaxPercent,
                    Count = item.ItemQuantity,
                    DiscountPercent = order.DiscountPercent                    
                });
            }

            return bc;
        }
    }
}
