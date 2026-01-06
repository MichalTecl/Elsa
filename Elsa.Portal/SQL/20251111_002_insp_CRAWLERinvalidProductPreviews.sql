CREATE OR ALTER PROCEDURE insp_CRAWLERinvalidProductPreviews (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN

    BEGIN TRY
        
        DECLARE @json nvarchar(max);
        DECLARE @fileExists INT;
        EXEC master.dbo.xp_fileexist 'C:\Elsa\ExtData\CrawlerResults\biorythmecz\invalidProductPreviews.json', @fileExists OUTPUT;
        IF (@fileExists = 0)
            RETURN;

        -- Načtení JSON souboru jako text (ne-Unicode)
        SELECT @json = CAST(BulkColumn AS nvarchar(max))
        FROM OPENROWSET(
            BULK 'C:\Elsa\ExtData\CrawlerResults\biorythmecz\invalidProductPreviews.json',
            SINGLE_CLOB
        ) AS src;

        IF @json IS NULL OR LEN(@json) = 0
        BEGIN            
            RETURN;
        END;

        -- Parsování JSON pole objektů a návrat výsledků
        SELECT 
            N'Neplatný náhled produktu v Blogu' IssueType,
            'invalidProductPreview_' + CONVERT(varchar(64), HASHBYTES('SHA2_256', j.[page] + '|' + j.[invalidUrl]), 2) IssueCode,
            N'Neplatný náhled produktu "' + j.[invalidUrl] + '" na stránce "' + j.[page] + '"' Message
        FROM OPENJSON(@json)
        WITH (
            [page]     nvarchar(500) '$.page',
            [invalidUrl]    nvarchar(500) '$.invalidUrl'
        ) AS j;
    END TRY
    BEGIN CATCH
        RETURN;
    END CATCH
END