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
    INSERT INTO UserRight (Symbol, Description)
	SELECT src.Val, src.Val
	  FROM @rights src
	 WHERE src.Val NOT IN (SELECT Symbol FROM UserRight);

	 DELETE FROM UserRoleRight 
	 WHERE RightId IN (
		SELECT r.Id
		  FROM UserRight r
		 WHERE r.Symbol NOT IN (SELECT Val FROM @rights));
	 
	 DELETE FROM UserRight 
	   WHERE Symbol NOT IN (SELECT Val FROM @rights);

	 SELECT Id, Symbol
	   FROM UserRight;
END
