IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'ListReportProcedures')
	DROP PROCEDURE ListReportProcedures;

GO

CREATE PROCEDURE ListReportProcedures
AS
BEGIN
	DECLARE @proclist TABLE (ProcedureName NVARCHAR(1000));
	DECLARE @result TABLE (ProcedureName NVARCHAR(1000), Title NVARCHAR(1000));
	DECLARE @spCode TABLE (ln NVARCHAR(1000));

	INSERT INTO @proclist 
	SELECT name FROM sys.procedures sps WHERE sps.name LIKE 'xrep_%';

	WHILE EXISTS(SELECT TOP 1 1 FROM @proclist)
	BEGIN
		DECLARE @spName NVARCHAR(1000) = (SELECT TOP 1 ProcedureName FROM @proclist);
		DELETE FROM @proclist WHERE ProcedureName = @spName;

		DELETE FROM @spCode;
		INSERT INTO @spCode
		EXEC sp_helptext @spName;

		DECLARE @spTitle NVARCHAR(1000)= (SELECT TOP 1 ln FROM @spCode WHERE ln LIKE '%Title:%');

		SET @spTitle = ISNULL(@spTitle, @spName);
		SET @spTitle = REPLACE(@spTitle, '/*', '');
		SET @spTitle = REPLACE(@spTitle, '*/', '');
		SET @spTitle = REPLACE(@spTitle, '--', '');
		SET @spTitle = REPLACE(@spTitle, 'Title:', '');
		SET @spTitle = TRIM(@spTitle);
	
		INSERT INTO @result 
		SELECT @spName, @spTitle;
	END

	SELECT * FROM @result ORDER BY Title;
END