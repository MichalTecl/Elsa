IF NOT EXISTS(SELECT TOP 1 1 FROM ReleasingFormsGenerationTask WHERE GeneratorName = 'DIRECT_SALES')
BEGIN
	INSERT INTO ReleasingFormsGenerationTask (FormText, GeneratorName, CounterId, ProjectId)
	VALUES (N'Přímý prodej', 'DIRECT_SALES', 2, 1);

	DECLARE @generatorId INT = SCOPE_IDENTITY();	
END