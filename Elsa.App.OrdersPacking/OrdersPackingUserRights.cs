using Elsa.Common.Interfaces;

namespace Elsa.App.OrdersPacking
{
    public static class OrdersPackingUserRights
    {
        public static readonly UserRight ViewOrdersPackingWidget = new UserRight(nameof(ViewOrdersPackingWidget), "Zobrazení panelu \"Zpracování objednávek\"");
        public static readonly UserRight DownloadTrackingDocument = new UserRight(nameof(DownloadTrackingDocument), "Stažení dokumentu pro Zásilkovnu", ViewOrdersPackingWidget);
        public static readonly UserRight OpenOrderPackingApplication = new UserRight(nameof(OpenOrderPackingApplication), "Otevření objednávky pro balení", ViewOrdersPackingWidget);
        public static readonly UserRight OrderBatchAssignment = new UserRight(nameof(OrderBatchAssignment), "Přiřazení šarží k objednávkám", OpenOrderPackingApplication);
        public static readonly UserRight MarkOrderPacked = new UserRight(nameof(MarkOrderPacked), "Zabalení objednávky", OrderBatchAssignment);
    }
}
