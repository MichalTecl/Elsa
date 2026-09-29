IF OBJECT_ID(N'dbo.xrep_ordersByDaysAndHours', N'P') IS NOT NULL
    DROP PROCEDURE dbo.xrep_ordersByDaysAndHours;
GO

CREATE PROCEDURE dbo.xrep_ordersByDaysAndHours (@projectId INT)
AS
BEGIN
    /*Title: Počty objednávek po dnech a hodinách */

    SET NOCOUNT ON;

    WITH grouped AS
    (
        SELECT
            (DATEDIFF(DAY, 0, po.PurchaseDate) % 7) + 1 AS WeekdayNum,
            DATEPART(HOUR, po.PurchaseDate) AS DayHour,
            COUNT(*) AS OrderCount
        FROM dbo.PurchaseOrder po
        WHERE po.PurchaseDate > DATEADD(DAY, -3650, GETDATE())
        GROUP BY
            (DATEDIFF(DAY, 0, po.PurchaseDate) % 7) + 1,
            DATEPART(HOUR, po.PurchaseDate)
    ),
    withMaximum AS
    (
        SELECT
            g.WeekdayNum,
            g.DayHour,
            g.OrderCount,
            MAX(g.OrderCount) OVER (PARTITION BY g.WeekdayNum) AS DayMaximum
        FROM grouped g
    )
    SELECT
        wd.Dtext AS [Den],
        RIGHT('0' + CONVERT(varchar(2), g.DayHour), 2) AS [Hodina],
        g.OrderCount AS [Objednávky],
        CASE
            WHEN g.OrderCount = g.DayMaximum THEN N'Denní MAX'
            ELSE N''
        END AS [MX]
    FROM withMaximum g
    JOIN
    (
        VALUES
            (1, N'Pondělí'),
            (2, N'Úterý'),
            (3, N'Středa'),
            (4, N'Čtvrtek'),
            (5, N'Pátek'),
            (6, N'Sobota'),
            (7, N'Neděle')
    ) wd (Dnum, Dtext) ON wd.Dnum = g.WeekdayNum
    ORDER BY
        g.WeekdayNum,
        g.DayHour;
END;
GO
