IF OBJECT_ID('dbo.GetDistributorOrdersOverview', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetDistributorOrdersOverview;
GO

CREATE PROCEDURE dbo.GetDistributorOrdersOverview
    @customerId INT,
    @pageSize INT,
    @lastSeenId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP(@pageSize)
           po.Id,
           po.OrderNumber,
           opi.PriceWithoutTax,
           po.PurchaseDate,
           po.ErpStatusName AS [Status],
           CASE
             WHEN po.PercentDiscountText IS NOT NULL AND po.DiscountsText IS NOT NULL
               THEN po.PercentDiscountText + ', ' + po.DiscountsText
             ELSE ISNULL(po.PercentDiscountText, po.DiscountsText)
           END AS Discounts
      FROM PurchaseOrder po
      JOIN Customer c ON (po.CustomerErpUid = c.ErpUid)
      JOIN vwOrderPriceInfo opi ON (opi.Id = po.Id)
      WHERE c.Id = @customerId
        AND (@lastSeenId IS NULL OR po.Id < @lastSeenId)
      ORDER BY po.Id DESC;
END;
GO
