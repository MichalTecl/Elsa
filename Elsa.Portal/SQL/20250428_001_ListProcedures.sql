IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'ListProcedures')
	DROP PROCEDURE ListProcedures;

GO

CREATE PROCEDURE ListProcedures(@like varchar(100))
AS
BEGIN
	DECLARE @proclist TABLE (ProcedureName NVARCHAR(1000));
	DECLARE @result TABLE (ProcedureName NVARCHAR(1000), Code NVARCHAR(MAX), Params NVARCHAR(MAX));
	DECLARE @spCode TABLE (id int identity(1,1), ln NVARCHAR(1000));

	INSERT INTO @proclist 
	SELECT name FROM sys.procedures sps WHERE sps.name LIKE @like;

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
		SELECT @spName, @spCodeFull, ISNULL(STRING_AGG(par.name, ';'), '')
		  FROM sys.procedures sp
		  LEFT JOIN sys.parameters par ON (par.object_id = sp.object_id)
		 WHERE sp.name = @spName
	END

	SELECT * FROM @result;
END
GO
