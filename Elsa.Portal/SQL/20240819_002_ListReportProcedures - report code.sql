IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'ListReportProcedures')
	DROP PROCEDURE ListReportProcedures;

GO

CREATE PROCEDURE ListReportProcedures
AS
BEGIN
	DECLARE @proclist TABLE (ProcedureName NVARCHAR(1000));
	DECLARE @result TABLE (ProcedureName NVARCHAR(1000), Code NVARCHAR(MAX));
	DECLARE @spCode TABLE (id int identity(1,1), ln NVARCHAR(1000));

	INSERT INTO @proclist 
	SELECT name FROM sys.procedures sps WHERE sps.name LIKE 'xrep_%';

	WHILE EXISTS(SELECT TOP 1 1 FROM @proclist)
	BEGIN
		DECLARE @spName NVARCHAR(1000) = (SELECT TOP 1 ProcedureName FROM @proclist);
		DELETE FROM @proclist WHERE ProcedureName = @spName;

		DELETE FROM @spCode;
		INSERT INTO @spCode (ln)
		EXEC sp_helptext @spName;

		WHILE (1 = 1)
		BEGIN
			DECLARE @codeLen INT = (SELECT SUM(LEN(ln)) FROM @spCode);

			PRINT(@codeLen);

			IF (@codeLen <= 2000)
				BREAK;
		
			DELETE FROM @spCode WHERE id = (SELECT MAX(id) FROM @spCode);
		END

		DECLARE @spCodeFull NVARCHAR(MAX) = (SELECt STRING_AGG(ln, CHAR(13) + CHAR(10)) FROM @spCode);
		
		INSERT INTO @result 
		SELECT @spName, @spCodeFull;
	END

	SELECT * FROM @result;
END