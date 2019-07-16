DECLARE @projectId INT = 1;

DECLARE @counterId INT;

IF NOT EXISTS(SELECT TOP 1 1 FROM SystemCounter WHERE Name = 'ReleaseForms_Default')
BEGIN
	INSERT INTO SystemCounter (Name, StaticPrefix, DtFormat, CounterValue, CounterMinValue, CounterPadding, ProjectId)
	VALUES ('ReleaseForms_Default', 'V', 'yyyy', 0, 1, 5, @projectId);	
END

SELECT TOP 1 @counterId = Id FROM SystemCounter WHERE [Name] = 'ReleaseForms_Default';


IF NOT EXISTS(SELECT TOP 1 1 FROM ReleasingFormsGenerationTask t WHERE t.GeneratorName = 'COMPOSITIONS')
BEGIN
	INSERT INTO ReleasingFormsGenerationTask (FormText, ProjectId, GeneratorName, CounterId)
	VALUES (N'VÝROBA', @projectId, 'COMPOSITIONS', @counterId);
END
