-- Drop the procedure if it already exists
IF OBJECT_ID('dbo.CrmLoadSalesStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.CrmLoadSalesStats;
GO

CREATE PROCEDURE dbo.CrmLoadSalesStats
AS
BEGIN
    SET NOCOUNT ON;

    WITH VocData AS (
        SELECT 
            CustomerId,
            VOC,
            PurchaseDate
        FROM vwOrderVocMoc
    ),
    Aggregated AS (
        SELECT 
            CustomerId,

            CAST(SUM(CASE WHEN YEAR(PurchaseDate) = YEAR(GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_ThisYear,
            CAST(SUM(CASE WHEN YEAR(PurchaseDate) = YEAR(GETDATE()) - 1 THEN VOC ELSE 0 END) AS INT) AS Voc_LastYear,

            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(DAY, -30, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Last30Days,

            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -12, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Last12M,
            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -24, GETDATE()) 
                                AND PurchaseDate < DATEADD(MONTH, -12, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Prev12M,

            CAST(SUM(CASE 
                        WHEN DATEPART(QUARTER, PurchaseDate) = DATEPART(QUARTER, GETDATE())
                             AND YEAR(PurchaseDate) = YEAR(GETDATE()) THEN VOC 
                        ELSE 0 
                    END) AS INT) AS Voc_ThisQuarter,
            CAST(SUM(CASE 
                        WHEN DATEPART(QUARTER, PurchaseDate) = DATEPART(QUARTER, DATEADD(QUARTER, -1, GETDATE()))
                             AND YEAR(PurchaseDate) = YEAR(DATEADD(QUARTER, -1, GETDATE())) THEN VOC 
                        ELSE 0 
                    END) AS INT) AS Voc_LastQuarter,

            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -3, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Last3M,
            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -6, GETDATE()) 
                                AND PurchaseDate < DATEADD(MONTH, -3, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Prev3M,

            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -6, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Last6M,
            CAST(SUM(CASE WHEN PurchaseDate >= DATEADD(MONTH, -12, GETDATE()) 
                                AND PurchaseDate < DATEADD(MONTH, -6, GETDATE()) THEN VOC ELSE 0 END) AS INT) AS Voc_Prev6M
        FROM VocData
        GROUP BY CustomerId
    ),
    Final AS (
        SELECT 
            *,
            CASE 
                WHEN Voc_LastYear = 0 AND Voc_ThisYear > 0 THEN -100
                WHEN Voc_LastYear = 0 AND Voc_ThisYear = 0 THEN 0
                ELSE CAST(ROUND((Voc_ThisYear - Voc_LastYear) * 100.0 / NULLIF(Voc_LastYear, 0), 0) AS INT)
            END AS Voc_ThisVsLastYear_Pct,

            CASE 
                WHEN Voc_Prev12M = 0 AND Voc_Last12M > 0 THEN -100
                WHEN Voc_Prev12M = 0 AND Voc_Last12M = 0 THEN 0
                ELSE CAST(ROUND((Voc_Last12M - Voc_Prev12M) * 100.0 / NULLIF(Voc_Prev12M, 0), 0) AS INT)
            END AS Voc_Last12VsPrev12_Pct
        FROM Aggregated
    )
    SELECT *
    FROM Final;
END
GO
