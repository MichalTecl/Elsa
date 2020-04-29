IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'GetFinFormsGenerationOverview')
BEGIN
	DROP PROCEDURE GetFinFormsGenerationOverview;
END

GO 

CREATE PROCEDURE GetFinFormsGenerationOverview (@projectId INT, @year INT, @month INT)
AS
BEGIN

	SELECT x.formTypeId, 
		   x.FormTypeName, 		   
		   x.GeneratorName,
		   CONVERT(BIT, x.IsGenerated) IsGenerated,
		   CONVERT(BIT,x.IsApproved) IsApproved,
		   CONVERT(BIT,CASE WHEN (x.IsGenerated = 1 AND x.HasProblems = 0 AND x.IsApproved = 0) THEN 1 ELSE 0 END) CanApprove,
		   x.CollectionId
	FROM (
		SELECT ift.Id FormTypeId,
			   ift.Name FormTypeName,
			   ift.GeneratorName GeneratorName,
			   CASE WHEN ifc.Id IS NULL THEN 0 ELSE 1 END IsGenerated,
			   CASE WHEN ifc.ApproveDt IS NULL THEN 0 ELSE 1 END IsApproved,		   
			   CASE WHEN EXISTS(SELECT TOP 1 1 FROM InvoiceFormGenerationLog ifgl WHERE ifgl.InvoiceFormCollectionId = ifc.Id AND ApproveDt IS NULL AND (ifgl.IsError = 1 OR ifgl.IsWarning = 1)) THEN 1 ELSE 0 END [HasProblems],
			   ifc.Id CollectionId

		  FROM InvoiceFormType ift
		  LEFT JOIN InvoiceFormCollection ifc ON (    ifc.InvoiceFormTypeId = ift.Id 
												  AND ifc.ProjectId = @projectId
												  AND ifc.Year = @year
												  AND ifc.Month = @month)      
          WHERE ift.ProjectId = @projectId
	) x  
END



 
