CREATE OR ALTER PROCEDURE [dbo].[GetMaterialLevelsReport](
     @inventoryId INT,
     @projectId INT,
     @materialLevelReportingGroup NVARCHAR(256) = NULL)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #Materials
    (
        MaterialId INT NOT NULL PRIMARY KEY,
        MaterialName NVARCHAR(256) NOT NULL,
        NominalUnitId INT NOT NULL
    );

    INSERT INTO #Materials
    (
        MaterialId,
        MaterialName,
        NominalUnitId
    )
    SELECT
        m.Id,
        m.Name,
        m.NominalUnitId
    FROM dbo.Material m
    WHERE m.InventoryId = @inventoryId
      AND m.ProjectId = @projectId
      AND
      (
            (
                @materialLevelReportingGroup IS NULL
                AND NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') IS NULL
            )
            OR NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') = @materialLevelReportingGroup
      );

    CREATE TABLE #Available
    (
        MaterialId INT NOT NULL,
        MaterialName NVARCHAR(256) NOT NULL,
        BatchNumber NVARCHAR(64) NULL,
        UnitId INT NOT NULL,
        Available DECIMAL(19, 4) NOT NULL
    );

    INSERT INTO #Available
    (
        MaterialId,
        MaterialName,
        BatchNumber,
        UnitId,
        Available
    )
    SELECT
        m.MaterialId,
        m.MaterialName,
        mb.BatchNumber,
        ISNULL(bam.UnitId, m.NominalUnitId) AS UnitId,
        SUM(ISNULL(bam.Available, 0)) AS Available
    FROM #Materials m
    JOIN dbo.vwBatchAvailableAmountWithoutSpentBatches bam
        ON bam.MaterialId = m.MaterialId
       AND bam.ProjectId = @projectId
       AND bam.InventoryId = @inventoryId
       AND bam.Available > 0
    JOIN dbo.MaterialBatch mb
        ON mb.Id = bam.BatchId
       AND mb.CloseDt IS NULL
    GROUP BY
        m.MaterialId,
        m.MaterialName,
        mb.BatchNumber,
        ISNULL(bam.UnitId, m.NominalUnitId);

    CREATE NONCLUSTERED INDEX IX_Available_MaterialId
    ON #Available (MaterialId);

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
            a.MaterialId,
            a.MaterialName,
            a.BatchNumber,
            a.UnitId,
            ISNULL(a.Available, 0) AS Available
        FROM #Available a

        UNION ALL

        SELECT
            m.MaterialId,
            m.MaterialName,
            CAST(NULL AS NVARCHAR(64)) AS BatchNumber,
            m.NominalUnitId AS UnitId,
            CAST(0 AS DECIMAL(19, 4)) AS Available
        FROM #Materials m
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM #Available a
            WHERE a.MaterialId = m.MaterialId
        )
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