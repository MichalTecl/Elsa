CREATE FUNCTION ParseBatchKey 
(	
	@batchKey nvarchar(100)
)
RETURNS TABLE 
AS
RETURN
	SELECT 
	CASE WHEN CHARINDEX(':', @batchKey) > 1 
		THEN
			SUBSTRING(@batchKey, 1, CHARINDEX(':', @batchKey) - 1) 
		ELSE
			NULL
		END
			as BatchNumber,
	CASE WHEN CHARINDEX(':', @batchKey) > 1 
		THEN
			CONVERT(INT, SUBSTRING(@batchKey, CHARINDEX(':', @batchKey) + 1, 9999)) 
		ELSE
			NULL
		END as MaterialId;

GO
