IF NOT EXISTS(SELECT TOP 1 1 FROM sys.tables WHERE name = N'CouponValidationRule')
BEGIN
    CREATE TABLE CouponValidationRule
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleName NVARCHAR(256) NOT NULL,
        RuleJson NVARCHAR(MAX) NOT NULL,
        ValidFrom DATETIME NULL,
        ValidTo DATETIME NULL,
        AuthorId INT NOT NULL
    );
END;

INSERT INTO AppWidget (Name, IsAnonymous, ViewOrder, WidgetUrl, UserRightSymbol)
SELECT N'EshopExtensionsWidget', 0, 7, N'/UI/EshopExtensions/EshopExtensionsWidget.html', N'ViewEshopExtensionsWidget'
WHERE NOT EXISTS(SELECT TOP 1 1 FROM AppWidget WHERE Name = N'EshopExtensionsWidget');
