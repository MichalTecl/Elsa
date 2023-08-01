IF TYPE_ID(N'StringTable') IS NULL
BEGIN
	CREATE TYPE StringTable AS TABLE (Val NVARCHAR(255));
END


IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'SyncUserRights')
	DROP PROCEDURE SyncUserRights;

GO

CREATE PROCEDURE SyncUserRights(@rights StringTable READONLY)
AS
BEGIN
    -- Update existing symbols and insert new ones
    MERGE INTO UserRight AS target
    USING (
        SELECT DISTINCT src.Val AS Symbol, src.Val AS Description
        FROM @rights src
    ) AS source ON target.Symbol = source.Symbol
    WHEN MATCHED THEN
        UPDATE SET Description = source.Description
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Symbol, Description)
        VALUES (source.Symbol, source.Description);

    -- Delete UserRoleRight for rights that no longer exist
    DELETE ur
    FROM UserRoleRight ur
    LEFT JOIN UserRight r ON ur.RightId = r.Id
    WHERE r.Id IS NULL;

    -- Delete UserRight for rights that no longer exist
    DELETE ur
    FROM UserRight ur
    LEFT JOIN @rights r ON ur.Symbol = r.Val
    WHERE r.Val IS NULL;

    -- Return the updated UserRight table
    SELECT Id, Symbol
    FROM UserRight;
END
