CREATE OR ALTER PROCEDURE [dbo].[GetMaterialLevelsReport](
     @inventoryId INT,
     @projectId INT,
     @materialLevelReportingGroup NVARCHAR(256) = NULL)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        y.MaterialId,
        y.MaterialName,
        y.BatchNumber,
        y.UnitId,
        y.Available,
        sup.Name AS SupplierName,
        sup.ContactEmail AS SupplierEmail,
        sup.ContactPhone AS SupplierPhone,
        orderEvent.OrderDt AS OrderDt,
        orderEvent.UserId AS OrderEventUserId,
        orderEvent.DeliveryDeadline AS DeliveryDeadline
    FROM
    (
        SELECT
            x.MaterialId,
            x.MaterialName,
            x.BatchNumber,
            x.UnitId,
            SUM(x.Available) AS Available
        FROM
        (
            SELECT
                m.Id AS MaterialId,
                m.Name AS MaterialName,
                mb.BatchNumber AS BatchNumber,
                ISNULL(bam.UnitId, m.NominalUnitId) AS UnitId,
                bam.Available AS Available
            FROM dbo.Material m
            INNER JOIN dbo.vwBatchAvailableAmountWithoutSpentBatches bam
                ON bam.MaterialId = m.Id
               AND bam.ProjectId = @projectId
               AND bam.InventoryId = @inventoryId
               AND bam.Available > 0
            INNER JOIN dbo.MaterialBatch mb
                ON mb.Id = bam.BatchId
               AND mb.CloseDt IS NULL
            WHERE m.InventoryId = @inventoryId
              AND m.ProjectId = @projectId
              AND (
                    (
                        @materialLevelReportingGroup IS NULL
                        AND NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') IS NULL
                    )
                    OR NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') = @materialLevelReportingGroup
                  )
        ) x
        GROUP BY
            x.MaterialId,
            x.MaterialName,
            x.BatchNumber,
            x.UnitId
    ) y
    LEFT JOIN dbo.vwMaterialSupplier msup
        ON y.MaterialId = msup.MaterialId
    LEFT JOIN dbo.Supplier sup
        ON msup.SupplierId = sup.Id
    OUTER APPLY
    (
        SELECT TOP (1)
            oe.OrderDt,
            oe.UserId,
            oe.DeliveryDeadline
        FROM dbo.MaterialOrderEvent oe
        WHERE oe.MaterialId = y.MaterialId
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.MaterialBatch b
              WHERE b.MaterialId = oe.MaterialId
                AND b.Created > oe.OrderDt
          )
        ORDER BY
            oe.OrderDt DESC,
            oe.Id DESC
    ) orderEvent
    ORDER BY
        y.MaterialName,
        y.BatchNumber;
END
GO