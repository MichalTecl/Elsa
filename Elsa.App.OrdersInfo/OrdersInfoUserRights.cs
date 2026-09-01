using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.App.OrdersInfo
{
    [UserRights]
    public static class OrdersInfoUserRights
    {
        public static readonly UserRight OrdersInfoAppView = new UserRight(nameof(OrdersInfoAppView), "Objednávky - Přístup do aplikace");
        public static readonly UserRight EditOrderItemBatchAssignments = new UserRight(
            nameof(EditOrderItemBatchAssignments),
            "Objednávky - Změna přiřazení šarží",
            OrdersInfoAppView);
    }
}
