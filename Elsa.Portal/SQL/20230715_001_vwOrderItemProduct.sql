IF OBJECT_ID('vwOrderItemProduct', 'V') IS NOT NULL
    DROP VIEW vwOrderItemProduct;
GO

CREATE VIEW vwOrderItemProduct AS
SELECT oi.Id AS OrderItemId, itemMaterial.MaterialId, ISNULL(map.ItemName, oi.PlacedName) AS EshopName
FROM OrderItem oi
JOIN (
    SELECT oimb.OrderItemId, MAX(mb.MaterialId) AS MaterialId
    FROM OrderItemMaterialBatch oimb
    JOIN MaterialBatch mb ON oimb.MaterialBatchId = mb.Id
    GROUP BY oimb.OrderItemId
) itemMaterial ON oi.Id = itemMaterial.OrderItemId
LEFT JOIN (
    SELECT vpm.ComponentId, MAX(map.Id) AS MappingId
    FROM VirtualProductMaterial vpm
    JOIN VirtualProductOrderItemMapping map ON vpm.VirtualProductId = map.VirtualProductId
    GROUP BY vpm.ComponentId
) m ON itemMaterial.MaterialId = m.ComponentId
LEFT JOIN VirtualProductOrderItemMapping map ON m.MappingId = map.Id;
