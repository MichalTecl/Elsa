IF OBJECT_ID('dbo.GetProductMaterialMappingCandidates', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetProductMaterialMappingCandidates;

GO

CREATE PROCEDURE GetProductMaterialMappingCandidates (@erpId INT, @includeHistoricalProducts BIT)
AS
BEGIN
    WITH mapped
    AS
    (
        SELECT map.ItemName ErpProductName,
               vpm.ComponentId MaterialId
          FROM VirtualProductOrderItemMapping map
          JOIN VirtualProduct vp ON (map.VirtualProductId = vp.Id)
          JOIN VirtualProductMaterial vpm ON (vpm.VirtualProductId = vp.Id)
         WHERE map.ErpId IS NULL
            OR map.ErpId = @erpId
    ),
    excludedProducts
    AS
    (
        SELECT kd.ItemName ProductName
          FROM KitDefinition kd
         WHERE kd.ErpId IS NULL
            OR kd.ErpId = @erpId
    ),
    historicalProducts
    AS
    (
        SELECT oi.PlacedName ProductName
          FROM OrderItem oi
          JOIN PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
         GROUP BY oi.PlacedName
        HAVING MAX(po.PurchaseDate) <= DATEADD(MONTH, -3, GETDATE())
    )
    SELECT mapped.MaterialId,
           m.Name MaterialName,
           mapped.ErpProductName ProductName
      FROM mapped
      LEFT JOIN Material m ON (mapped.MaterialId = m.Id)
     WHERE mapped.ErpProductName NOT IN (SELECT ep.ProductName FROM excludedProducts ep)

    UNION

    SELECT NULL MaterialId,
           NULL MaterialName,
           oi.PlacedName ProductName
      FROM OrderItem oi
      JOIN PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
     WHERE po.PurchaseDate > DATEADD(YEAR, -1, GETDATE())
       AND oi.PlacedName NOT IN (SELECT m.ErpProductName FROM mapped m)
       AND oi.PlacedName NOT IN (SELECT ep.ProductName FROM excludedProducts ep)
       AND (@includeHistoricalProducts = 1 OR oi.PlacedName NOT IN (SELECT hp.ProductName FROM historicalProducts hp))

    UNION

    SELECT NULL MaterialId,
           NULL MaterialName,
           p.Name ProductName
      FROM Product p
     WHERE p.Name NOT IN (SELECT m.ErpProductName FROM mapped m)
       AND p.Name NOT IN (SELECT ep.ProductName FROM excludedProducts ep)
       AND (@includeHistoricalProducts = 1 OR p.Name NOT IN (SELECT hp.ProductName FROM historicalProducts hp))

    UNION

    SELECT m.Id MaterialId,
           m.Name MaterialName,
           NULL ProductName
      FROM Material m
      JOIN MaterialInventory i ON (m.InventoryId = i.Id)
     WHERE i.CanBeConnectedToTag = 1
       AND m.HideDt IS NULL
       AND m.Id NOT IN (SELECT mp.MaterialId FROM mapped mp);
END
GO
