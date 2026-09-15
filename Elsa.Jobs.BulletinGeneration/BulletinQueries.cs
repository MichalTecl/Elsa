namespace Elsa.Jobs.BulletinGeneration
{
    internal static class BulletinQueries
    {
        // One row per order. The first item supplies the agreed single VAT rate.
        internal const string OrderFrom = @"
            FROM PurchaseOrder po
            OUTER APPLY (
                SELECT MAX(CAST(customer.IsDistributor AS int)) AS IsDistributor
                FROM Customer customer
                WHERE customer.ErpUid = po.CustomerErpUid AND customer.ProjectId = po.ProjectId
            ) c
            OUTER APPLY (
                SELECT TOP (1) oi.TaxPercent FROM OrderItem oi
                WHERE oi.PurchaseOrderId = po.Id ORDER BY oi.Id
            ) firstItem
            CROSS APPLY (
                SELECT (po.PriceWithVat - po.TaxedShippingCost - po.TaxedPaymentCost)
                    / NULLIF(1 + firstItem.TaxPercent / 100.0, 0) AS NetRevenue
            ) revenue";

        internal const string CustomerKey = @"CASE
            WHEN NULLIF(LTRIM(RTRIM(po.CustomerErpUid)), '') IS NOT NULL
                THEN 'uid:' + LOWER(LTRIM(RTRIM(po.CustomerErpUid)))
            WHEN NULLIF(LTRIM(RTRIM(po.CustomerEmail)), '') IS NOT NULL
                THEN 'email:' + LOWER(LTRIM(RTRIM(po.CustomerEmail)))
            ELSE NULL END";
    }
}
