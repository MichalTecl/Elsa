using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Flox;

namespace Elsa.Commerce.Core
{
    public interface IErpClient
    {
        IErp Erp { get; set; }

        IErpDataMapper Mapper { get; }

        IErpCommonSettings CommonSettings { get; }

        IEnumerable<IErpOrderModel> LoadOrdersSnapshot(DateTime from, DateTime? to = null);

        IEnumerable<IErpOrderModel> LoadOrdersIncremental(DateTime changedAfter);

        IEnumerable<IErpOrderModel> LoadPaidOrders(DateTime from, DateTime to);

        void MarkOrderPaid(IPurchaseOrder po);

        IErpOrderModel LoadOrder(string orderNumber);

        DateTime ObtainOrderLastChange(string orderNumber);

        void MakeOrderSent(IPurchaseOrder po);

        IEnumerable<IErpCustomerModel> LoadCustomers();

        string GetPackingReferenceNumber(IPurchaseOrder po);
        string LoadOrderInternalNote(string orderNumber);

        ICollection<string> GetProductNames();
    }
}
