using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.App.OrdersPacking
{
    [UserRights]
    public static class OrdersPackingUserRights
    {
        public static readonly UserRight ViewOrdersPackingWidget = new UserRight(nameof(ViewOrdersPackingWidget), "Zpracování objednávek");
        public static readonly UserRight DownloadTrackingDocument = new UserRight(nameof(DownloadTrackingDocument), "Stažení dokumentů dopravců", ViewOrdersPackingWidget);
        public static readonly UserRight OpenOrderPackingApplication = new UserRight(nameof(OpenOrderPackingApplication), "Otevření objednávky pro balení", ViewOrdersPackingWidget);
        public static readonly UserRight OrderBatchAssignment = new UserRight(nameof(OrderBatchAssignment), "Přiřazení šarží k objednávkám", OpenOrderPackingApplication);
        public static readonly UserRight MarkOrderPacked = new UserRight(nameof(MarkOrderPacked), "Zabalení objednávky", OrderBatchAssignment);
    }
}
