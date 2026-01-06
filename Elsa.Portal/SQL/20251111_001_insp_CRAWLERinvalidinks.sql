CREATE OR ALTER PROCEDURE insp_CRAWLERinvalidLinks (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
    DECLARE @json nvarchar(max);

    BEGIN TRY
        DECLARE @fileExists INT;
        EXEC master.dbo.xp_fileexist 'C:\Elsa\ExtData\CrawlerResults\biorythmecz\invalidlinks.json', @fileExists OUTPUT;
        IF (@fileExists = 0)
            RETURN;

        -- Načtení JSON souboru jako text (ne-Unicode)
        SELECT @json = CAST(BulkColumn AS nvarchar(max))
        FROM OPENROWSET(
            BULK 'C:\Elsa\ExtData\CrawlerResults\biorythmecz\invalidlinks.json',
            SINGLE_CLOB
        ) AS src;

        IF @json IS NULL OR LEN(@json) = 0
        BEGIN            
            RETURN;
        END;

        -- Parsování JSON pole objektů a návrat výsledků
        SELECT 
            N'Neplatný link v E-Shopu' IssueType,
            'invalidLink_' + CONVERT(varchar(64), HASHBYTES('SHA2_256', j.[at] + '|' + j.[url]), 2) IssueCode,
            'Neplatny link "' + j.[url] + '" na strance "' + j.[at] + '"' Message
        FROM OPENJSON(@json)
        WITH (
            [at]     nvarchar(500) '$.at',
            [url]    nvarchar(500) '$.url',
            [reason] nvarchar(max) '$.reason'
        ) AS j;
    END TRY
    BEGIN CATCH
        RETURN;
    END CATCH
END