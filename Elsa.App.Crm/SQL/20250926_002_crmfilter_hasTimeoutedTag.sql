
CREATE OR ALTER PROCEDURE [dbo].[crmfilter_hasTimeoutedTag]
AS
BEGIN
    /* Title:Mají štítek po timeoutu */
	/* Note:Vybere zákazníky, kteří mají alespoň jeden štítek po nastaveném timeoutu */
		
	SELECT c.Id
	  FROM Customer c
	  JOIN CustomerTagAssignment cta ON (cta.CustomerId = c.Id)
	  JOIN CustomerTagType tt ON (tt.Id = cta.TagTypeId)
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
      AND (cta.UnassignDt IS NULL OR cta.UnassignDt > GETDATE())
	  AND ISNULL(tt.DaysToWarning, 0) > 0	  
	  AND DATEADD(DAY, tt.DaysToWarning, cta.AssignDt) < GETDATE()

END
GO


